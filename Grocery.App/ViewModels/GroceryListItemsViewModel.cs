using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel; // voor MainThread
using System.Threading.Tasks;
using System;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;

        // interne volledige set van beschikbare producten (voor filtering)
        private List<Product> _allAvailableProducts = new();

        // ObservableCollections gebruikt door de view
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = new ObservableCollection<GroceryListItem>();
        public ObservableCollection<Product> AvailableProducts { get; set; } = new ObservableCollection<Product>();

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        [ObservableProperty]
        string myMessage;

        // Zoektekst; gebonden aan SearchBar.Text
        [ObservableProperty]
        string searchText;

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            // Bouw lijst van alle producten die niet in de boodschappenlijst zitten en stock > 0
            _allAvailableProducts.Clear();
            foreach (Product p in _productService.GetAll())
            {
                if (MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0)
                    _allAvailableProducts.Add(p);
            }

            // Update de ObservableCollection op de UI-thread
            UpdateAvailableProducts(SearchText);
        }

        // Update AvailableProducts op basis van (optionele) query
        private void UpdateAvailableProducts(string query = null)
        {
            var q = (query ?? string.Empty).Trim().ToLowerInvariant();

            IEnumerable<Product> items;
            if (string.IsNullOrWhiteSpace(q))
            {
                items = _allAvailableProducts;
            }
            else
            {
                items = _allAvailableProducts.Where(p =>
                    !string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(q)
                );
            }

            // Update op main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AvailableProducts.Clear();
                foreach (var p in items)
                    AvailableProducts.Add(p);
            });
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);

            // Verwijder het product uit zowel de UI-collectie als de interne volledige lijst
            // Verwijder op main thread om veilig UI bij te werken
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AvailableProducts.Remove(product);
            });

            _allAvailableProducts.RemoveAll(p => p.Id == product.Id);

            OnGroceryListChanged(GroceryList);
        }

        // RelayCommand die door SearchBar wordt aangeroepen. De SearchCommand wordt gegenereerd door de brongenerator.
        [RelayCommand]
        public void Search(string query)
        {
            // Als query null is, probeer de property SearchText
            var q = query ?? SearchText;
            UpdateAvailableProducts(q);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

    }
}