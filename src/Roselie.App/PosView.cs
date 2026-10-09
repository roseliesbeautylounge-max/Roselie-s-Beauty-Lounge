using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Roselie.Core.Finance;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App;

public sealed class PosViewModel : Observable
{
 public ObservableCollection<CartLineInput> Lines { get; }=[];
 public Guid CheckoutToken { get; private set; }=Guid.NewGuid();
 public void NewOrder(){Lines.Clear();CheckoutToken=Guid.NewGuid();}
}

public sealed class PosView : Grid
{
 private readonly BusinessService _business;
 private readonly UserAccount _user;
 private readonly Action<Sale> _onComplete;
 private readonly PosViewModel _vm=new();
 private readonly WrapPanel _catalog=new();
 private readonly StackPanel _cart=new();
 private readonly TextBox _search=Ui.SearchBox("Search services, products or scan a barcode");
 private readonly ComboBox _category=new();
 private readonly StackPanel _categoryPills=new(){Orientation=Orientation.Horizontal};
 private readonly ComboBox _customer=new(){DisplayMemberPath="FullName"};
 private readonly TextBox _orderDiscount=Ui.PercentageInput(0,double.NaN);
 private readonly TextBox _discountReason=Ui.Input();
 private readonly TextBlock _total=Ui.Text("₱0.00",30,"#A85E67",true);
 private readonly TextBlock _error=Ui.Text("",12,"#C84D58");
 private readonly TextBlock _resultCount=Ui.Text("",12,"#77727B");
 private readonly TextBlock _cartBadgeText=new(){FontSize=12,FontWeight=FontWeights.Bold,Foreground=Ui.Brush("#A85E67")};
 private readonly TextBlock _subtotalValue=Ui.Text("₱0.00",13,"#344052",true);
 private readonly TextBlock _discountValue=Ui.Text("₱0.00",13,"#344052",true);
 private readonly TextBlock _catalogTitle=Ui.Text("Find the perfect service",22,"#344052",true);
 private readonly ColumnDefinition _cartColumn=new(){Width=new GridLength(380)};
 private readonly Button _checkout;
 private ScrollViewer? _catalogScroll;
 private readonly Button _btnServices;
 private readonly Button _btnProducts;
 private readonly Button _btnPackages;
 private readonly Button? _btnHistory;
 private string _mode="Services";
 private List<SalonService> _services=[];
 private List<ServiceCategory> _serviceCategories=[];
 private List<Product> _products=[];
 private List<ProductCategory> _productCategories=[];
 private List<PackageOffer> _packages=[];
 private bool _loading;

 public PosView(BusinessService business,UserAccount user,Action<Sale> onComplete,Action? history=null)
 {
  _business=business;_user=user;_onComplete=onComplete;DataContext=_vm;
  ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  ColumnDefinitions.Add(_cartColumn);

  var left=new DockPanel{Margin=new Thickness(0,0,18,0)};
  var header=Ui.Stack(
   _catalogTitle,
   Ui.Text("Personalize every visit. Start a transaction in just a few clicks.",13,"#77727B")
  );
  header.Margin=new Thickness(0,0,0,12);

  var modeRow=new Grid{Margin=new Thickness(0,12,0,10)};
  modeRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  modeRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var modes=new UniformGrid{Columns=3};
  _btnServices=Ui.Pill("Service Catalog",()=>SetMode("Services"),true);
  _btnProducts=Ui.Pill("Products",()=>SetMode("Products"));
  _btnPackages=Ui.Pill("Packages",()=>SetMode("Packages"));
  foreach(var button in new[]{_btnServices,_btnProducts,_btnPackages}){button.Margin=new Thickness(2);button.Padding=new Thickness(12,7,12,7);modes.Children.Add(button);}
  modeRow.Children.Add(new Border{Background=Ui.Brush("#F3E7E7"),CornerRadius=new CornerRadius(10),Padding=new Thickness(3),Child=modes});
  if(history!=null)
  {
   _btnHistory=Ui.Button("Transaction History",history);
   _btnHistory.Margin=new Thickness(10,0,0,0);
   _btnHistory.ToolTip="Review transactions and receipts";
   System.Windows.Automation.AutomationProperties.SetName(_btnHistory,"Transaction History");
   Grid.SetColumn(_btnHistory,1);modeRow.Children.Add(_btnHistory);
  }
  header.Children.Add(modeRow);

  var searchBoxWrapper=new Grid{Margin=new Thickness(0,0,0,10)};
  _search.ToolTip="Search name, SKU or scan barcode";
  _search.MinHeight=42;
  _search.Padding=new Thickness(36,9,42,9);
  searchBoxWrapper.Children.Add(_search);

  var clearSearchBtn=Ui.IconButton("\uE711","Clear search",()=>{_search.Clear();_search.Focus();});
  clearSearchBtn.Visibility=Visibility.Collapsed;clearSearchBtn.HorizontalAlignment=HorizontalAlignment.Right;clearSearchBtn.VerticalAlignment=VerticalAlignment.Center;clearSearchBtn.Margin=new Thickness(0,0,4,0);
  searchBoxWrapper.Children.Add(clearSearchBtn);

  var searchLabelRow=new Grid{Margin=new Thickness(0,0,0,6)};
  searchLabelRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  searchLabelRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  searchLabelRow.Children.Add(Ui.Text("Search name, SKU or scan barcode",12,"#77727B"));
  Grid.SetColumn(_resultCount,1);
  searchLabelRow.Children.Add(_resultCount);
  header.Children.Add(searchLabelRow);
  header.Children.Add(searchBoxWrapper);
  var pillsScroll=new ScrollViewer{Content=_categoryPills,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,Margin=new Thickness(0,0,0,2)};
  header.Children.Add(pillsScroll);

  DockPanel.SetDock(header,Dock.Top);
  left.Children.Add(header);
  _catalogScroll=Ui.Scroll(_catalog);
  _catalogScroll.SizeChanged+=(_,_)=>ResizeCatalogCards();
  _catalogScroll.ScrollChanged+=(_,e)=>{if(Math.Abs(e.ViewportWidthChange)>0.1)ResizeCatalogCards();};
  left.Children.Add(_catalogScroll);
  Children.Add(left);

  var order=new DockPanel();
  var cartBadgeBorder=new Border{Background=Ui.Brush("#F3E7E7"),CornerRadius=new CornerRadius(12),Padding=new Thickness(9,3,9,3),Child=_cartBadgeText,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,0)};
  var orderTitleRow=Ui.Row(Ui.Text("Current Transaction",20,"#344052",true),cartBadgeBorder);
  orderTitleRow.Margin=new Thickness(0,0,0,12);
  var customerRow=new Grid();customerRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});customerRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  customerRow.Children.Add(_customer);var addCustomer=Ui.Button("+",NewCustomer);addCustomer.ToolTip="Add a customer";addCustomer.Margin=new Thickness(6,0,0,0);addCustomer.Padding=new Thickness(12,7,12,7);Grid.SetColumn(addCustomer,1);customerRow.Children.Add(addCustomer);
  var orderTop=Ui.Stack(orderTitleRow,Ui.Field("Customer",customerRow));
  DockPanel.SetDock(orderTop,Dock.Top);
  order.Children.Add(orderTop);

  _orderDiscount.ToolTip=$"Maximum authorized discount: {_user.MaxDiscountPercent}%";
  var discountFields=Ui.Stack(Ui.Field("Transaction discount (%)",_orderDiscount),Ui.Field("Discount reason (optional)",_discountReason),Ui.Text("Use an item discount or a transaction discount. They cannot be combined.",11,"#77727B"));
  var discountExpander=new Expander{Header="Transaction discount",Content=discountFields,Margin=new Thickness(0,6,0,10),Foreground=Ui.Brush("#77727B")};
  _checkout=Ui.Button("Proceed to Payment",Checkout,true);_checkout.Margin=new Thickness(0,0,0,6);_checkout.MinHeight=46;
  var clearOrder=Ui.Button("New Transaction",()=>{if(_vm.Lines.Count>0 && Ui.Confirm("Start a new transaction?","Clear all items from the current transaction?","New Transaction")){_vm.NewOrder();_orderDiscount.Text="0";_discountReason.Clear();RefreshCart();LoadCatalog();Ui.Toast("Ready for a new transaction","info");}});
  clearOrder.Margin=new Thickness(0);clearOrder.MinHeight=32;clearOrder.Padding=new Thickness(10,5,10,5);
  var totalRow=new Grid{Margin=new Thickness(0,12,0,14)};totalRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});totalRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});var totalLabel=Ui.Text("Total due",15,"#344052",true);totalLabel.VerticalAlignment=VerticalAlignment.Center;totalRow.Children.Add(totalLabel);Grid.SetColumn(_total,1);totalRow.Children.Add(_total);
  var bottom=Ui.Stack(new Border{Height=1,Background=Ui.Brush("#E9DADB"),Margin=new Thickness(0,10,0,0)},discountExpander,SummaryRow("Subtotal",_subtotalValue),SummaryRow("Discounts",_discountValue),_error,totalRow,_checkout,clearOrder);
  DockPanel.SetDock(bottom,Dock.Bottom);
  order.Children.Add(bottom);
  order.Children.Add(Ui.Scroll(_cart));

  var card=Ui.Card(order,new Thickness(0));
  Grid.SetColumn(card,1);
  Children.Add(card);

  _search.TextChanged+=(_,_)=>{clearSearchBtn.Visibility=_search.Text.Length>0?Visibility.Visible:Visibility.Collapsed;LoadCatalog();};
  _search.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){var barcode=_search.Text.Trim();var product=_business.List<Product>(_user).FirstOrDefault(p=>p.IsActive&&p.IsRetail&&p.Barcode==barcode&&barcode.Length>0);if(product!=null){AddProduct(product);_search.Clear();e.Handled=true;}}};
  _category.SelectionChanged+=(_,_)=>{if(!_loading){UpdateCategoryPillSelection();LoadCatalog();}};
  _orderDiscount.TextChanged+=(_,_)=>UpdateTotal();
  SizeChanged+=(_,_)=>{_cartColumn.Width=new GridLength(ActualWidth<1000?350:380);if(_btnHistory!=null){_btnHistory.Content=ActualWidth<1000?Ui.Icon("\uE81C",17):"Transaction History";_btnHistory.Padding=ActualWidth<1000?new Thickness(10):new Thickness(16,9,16,9);}ResizeCatalogCards();};

  RefreshCatalog();RefreshCart();
 }

 private static Grid SummaryRow(string label,TextBlock value)
 {
  var row=new Grid{Margin=new Thickness(0,3,0,3)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});row.Children.Add(Ui.Text(label,13,"#77727B"));Grid.SetColumn(value,1);row.Children.Add(value);return row;
 }

 private void ResizeCatalogCards()
 {
  if(_catalogScroll==null || _catalogScroll.ActualWidth<=0)return;
  var viewport=_catalogScroll.ViewportWidth;
  var available=Math.Max(160,(double.IsFinite(viewport)&&viewport>0?viewport:_catalogScroll.ActualWidth-SystemParameters.VerticalScrollBarWidth)-2);
  var preferredWidth=ActualWidth<1000?180:200;
  var columns=Math.Max(1,(int)Math.Floor(available/(preferredWidth+12)));
  var cardWidth=Math.Max(148,(available-12*columns)/columns);
  foreach(FrameworkElement card in _catalog.Children)card.Width=cardWidth;
 }

 public void RefreshCatalog()
 {
  _services=_business.List<SalonService>(_user);
  _serviceCategories=_business.List<ServiceCategory>(_user);
  _products=_business.List<Product>(_user);
  _productCategories=_business.List<ProductCategory>(_user);
  _packages=_business.List<PackageOffer>(_user);
  var selected=(_customer.SelectedItem as Customer)?.Id;
  var customers=new List<Customer>{new(){Id=Guid.Empty,FullName="Walk-in customer"}};
  customers.AddRange(_business.List<Customer>(_user).Where(c=>c.IsActive).OrderBy(c=>c.FullName));
  _customer.ItemsSource=customers;
  _customer.SelectedItem=customers.FirstOrDefault(c=>c.Id==selected)??customers[0];
  SetMode(_mode);
 }

 private void SetMode(string mode)
 {
  _mode=mode;
  _loading=true;
  _btnServices.Style=(Style)FindResource(mode=="Services"?"ActivePillButton":"PillButton");
  _btnProducts.Style=(Style)FindResource(mode=="Products"?"ActivePillButton":"PillButton");
  _btnPackages.Style=(Style)FindResource(mode=="Packages"?"ActivePillButton":"PillButton");
  _catalogTitle.Text=mode switch{"Products"=>"A little care to take home","Packages"=>"More beautiful visits, together",_=>"Find the perfect service"};

  var categories=new List<string>{"All categories"};
  if(mode=="Services")categories.AddRange(_serviceCategories.Where(c=>c.IsActive).OrderBy(c=>c.DisplayOrder).Select(c=>c.Name));
  if(mode=="Products")categories.AddRange(_productCategories.Where(c=>c.IsActive).Select(c=>c.Name));
  _category.ItemsSource=categories;
  _category.SelectedIndex=0;
  BuildCategoryPills(categories);
  _loading=false;
  LoadCatalog();
 }

 private void BuildCategoryPills(List<string> categories)
 {
  _categoryPills.Children.Clear();
  var selected=_category.SelectedItem?.ToString()??"All categories";
  foreach(var cat in categories)
  {
   var isSel=cat==selected;
   var pill=Ui.Pill(cat,()=>{_category.SelectedItem=cat;UpdateCategoryPillSelection();LoadCatalog();},isSel);
   pill.MaxWidth=220;pill.ToolTip=cat;pill.Margin=new Thickness(0,0,6,6);
   _categoryPills.Children.Add(pill);
  }
 }

 private void UpdateCategoryPillSelection()
 {
  var selected=_category.SelectedItem?.ToString()??"All categories";
  foreach(Button pill in _categoryPills.Children)
  {
   var isSel=pill.Content?.ToString()==selected;
   pill.Style=(Style)FindResource(isSel?"ActivePillButton":"PillButton");
  }
 }

 private void LoadCatalog()
 {
  if(_category.ItemsSource==null)return;
  _catalog.Children.Clear();
  var term=_search.Text.Trim();
  var category=_category.SelectedItem?.ToString()??"All categories";
  int count=0;

  if(_mode=="Services")
  {
   var categories=_serviceCategories.ToDictionary(c=>c.Id,c=>c.Name);
   var list=_services.Where(s=>s.IsActive && s.Name.Contains(term,StringComparison.OrdinalIgnoreCase) && (category=="All categories"||categories.GetValueOrDefault(s.CategoryId)==category)).OrderBy(s=>_serviceCategories.FirstOrDefault(c=>c.Id==s.CategoryId)?.DisplayOrder??99).ThenBy(s=>s.Name).ToList();
   count=list.Count;
   foreach(var s in list)
   {
    var price=s.PricingType switch{PricingType.Range=>$"{Ui.Money(s.MinimumPrice??s.BasePrice)} – {Ui.Money(s.MaximumPrice??s.BasePrice)}",PricingType.StartsAt=>"From "+Ui.Money(s.BasePrice),_=>Ui.Money(s.BasePrice)};
    var inCart=_vm.Lines.Where(l=>l.CatalogItemId==s.Id).Sum(l=>l.Quantity);
    CatalogCard(s.Name,categories.GetValueOrDefault(s.CategoryId,"Service"),price,s.DurationMinutes>0?$"{s.DurationMinutes} min · {s.PricingType}":$"{s.PricingType} pricing",()=>Customize(new(){CatalogItemId=s.Id,Name=s.Name,Kind=SaleItemKind.Service,UnitPrice=s.BasePrice},true),true,inCart);
   }
  }
  else if(_mode=="Products")
  {
   var categories=_productCategories.ToDictionary(c=>c.Id,c=>c.Name);
   var list=_products.Where(p=>p.IsActive&&p.IsRetail&&(p.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||p.Sku.Contains(term,StringComparison.OrdinalIgnoreCase)||p.Barcode.Contains(term,StringComparison.OrdinalIgnoreCase))&&(category=="All categories"||categories.GetValueOrDefault(p.CategoryId??Guid.Empty)==category)).ToList();
   count=list.Count;
   foreach(var p in list)
   {
    var inCart=_vm.Lines.Where(l=>l.CatalogItemId==p.Id).Sum(l=>l.Quantity);
    CatalogCard(p.Name,string.IsNullOrWhiteSpace(p.Brand)?"Retail product":p.Brand,Ui.Money(p.SellingPrice),$"{p.StockQuantity:0.######} {p.StockUnit} in stock",()=>AddProduct(p),p.StockQuantity>0,inCart,p.ImagePath);
   }
  }
  else
  {
   var list=_packages.Where(p=>p.IsActive&&p.Name.Contains(term,StringComparison.OrdinalIgnoreCase)).ToList();
   count=list.Count;
   foreach(var p in list)
   {
    var inCart=_vm.Lines.Where(l=>l.CatalogItemId==p.Id).Sum(l=>l.Quantity);
    CatalogCard(p.Name,"PREPAID PACKAGE",Ui.Money(p.Price),$"{p.TotalSessions} sessions · customer required",()=>{if((_customer.SelectedItem as Customer)?.Id==Guid.Empty)throw new InvalidOperationException("Select a customer before selling a package.");Customize(new(){CatalogItemId=p.Id,Name=p.Name,Kind=SaleItemKind.Package,UnitPrice=p.Price},true);},true,inCart);
   }
  }
  _resultCount.Text=$"{count} item{(count==1?"":"s")}";
  if(_catalog.Children.Count==0)
  {
   var empty=Ui.Stack(Ui.Text(_mode=="Products"?"Your retail collection starts here":"No matching items",18,"#344052",true),Ui.Text(_mode=="Products"&&term.Length==0?"Add your products and stock through Products and Inventory.":"Try another name or select a different category.",13,"#77727B"));
   _catalog.Children.Add(Ui.Card(empty,new Thickness(0,8,0,0)));
  }
  ResizeCatalogCards();
 }

 private void CatalogCard(string name,string category,string price,string note,Action action,bool enabled=true,decimal inCart=0,string? imagePath=null)
 {
  var topGrid=new Grid{Height=32,Margin=new Thickness(0,0,0,8)};
  topGrid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  topGrid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var categoryText=Ui.Text(category.ToUpperInvariant(),10,"#A85E67",true);
  categoryText.MaxHeight=28;categoryText.TextTrimming=TextTrimming.CharacterEllipsis;categoryText.ToolTip=category;
  topGrid.Children.Add(categoryText);
  if(inCart>0)
  {
   var badge=Ui.Badge($"✓ {inCart:0.######}","#EAF5F0","#27845F",10,true);
   badge.ToolTip="Quantity in the current transaction";badge.Margin=new Thickness(5,0,0,0);
   Grid.SetColumn(badge,1);topGrid.Children.Add(badge);
  }

  var panel=new Grid();
  panel.RowDefinitions.Add(new(){Height=GridLength.Auto});
  panel.RowDefinitions.Add(new(){Height=GridLength.Auto});
  panel.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  panel.RowDefinitions.Add(new(){Height=GridLength.Auto});
  panel.Children.Add(topGrid);
  if(_mode=="Products")
  {
   UIElement visual;
   if(TryProductImage(imagePath) is Image productImage){productImage.Height=72;visual=productImage;}
   else
   {
    var icon=Ui.Icon("\uE719",28);icon.Foreground=Ui.Brush("#C8797D");icon.HorizontalAlignment=HorizontalAlignment.Center;icon.VerticalAlignment=VerticalAlignment.Center;
    visual=icon;
   }
   var imageFrame=new Border{Height=76,Background=Ui.Brush("#FAF6F5"),CornerRadius=new CornerRadius(8),Child=visual,Margin=new Thickness(0,0,0,8)};
   Grid.SetRow(imageFrame,1);panel.Children.Add(imageFrame);
  }
  var nameText=Ui.Text(name,16,"#344052",true);
  nameText.Height=42;nameText.TextTrimming=TextTrimming.CharacterEllipsis;nameText.ToolTip=name;nameText.Margin=new Thickness(0,0,0,6);
  var priceText=Ui.Text(price,17,"#A85E67",true);
  priceText.MaxHeight=42;priceText.TextTrimming=TextTrimming.CharacterEllipsis;priceText.ToolTip=price;priceText.Margin=new Thickness(0,0,0,6);
  var noteText=Ui.Text(note,12,"#77727B");
  noteText.MaxHeight=32;noteText.TextTrimming=TextTrimming.CharacterEllipsis;noteText.ToolTip=note;
  var details=Ui.Stack(nameText,priceText,noteText);details.ClipToBounds=true;
  Grid.SetRow(details,2);panel.Children.Add(details);
  var button=Ui.Button(_mode switch{"Products"=>"Add Product","Packages"=>"Add Package",_=>"Add Service"},action,inCart>0);
  button.IsEnabled=enabled;button.Margin=new Thickness(0,8,0,0);button.Padding=new Thickness(12,7,12,7);button.MinHeight=38;
  Grid.SetRow(button,3);panel.Children.Add(button);
  var card=Ui.InteractiveCard(panel,new Thickness(0,0,12,12));
  card.Width=215;card.Height=_mode=="Products"?318:234;card.Padding=new Thickness(16);
  if(!enabled){card.IsEnabled=false;card.Opacity=0.6;ToolTipService.SetShowOnDisabled(card,true);}
  card.Cursor=enabled?Cursors.Hand:Cursors.Arrow;
  card.ToolTip=enabled?"Add "+name+" to the current transaction":"This product is out of stock";
  card.MouseLeftButtonUp+=(_,e)=>{if(enabled){Ui.Guard(action);e.Handled=true;}};
  _catalog.Children.Add(card);
 }

 private static Image? TryProductImage(string? path)
 {
  if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))return null;
  try
  {
   var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=400;bitmap.UriSource=new Uri(Path.GetFullPath(path));bitmap.EndInit();bitmap.Freeze();
   return new Image{Source=bitmap,Height=76,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center};
  }
  catch{return null;}
 }

 private void AddProduct(Product product)
 {
  var existing=_vm.Lines.FirstOrDefault(x=>x.Kind==SaleItemKind.Product&&x.CatalogItemId==product.Id&&x.DiscountPercent==0);
  if(existing!=null)existing.Quantity++;
  else _vm.Lines.Add(new(){CatalogItemId=product.Id,Name=product.Name,Kind=SaleItemKind.Product,UnitPrice=product.SellingPrice});
  RefreshCart();
  LoadCatalog();
  Ui.Toast($"Added {product.Name} to the transaction");
 }

 private void RefreshCart()
 {
  _cart.Children.Clear();
  var totalItems=_vm.Lines.Sum(x=>x.Quantity);
  _cartBadgeText.Text=$"{totalItems:0.######} item{(totalItems==1?"":"s")}";

  if(_vm.Lines.Count==0)
  {
   _cart.Children.Add(Ui.EmptyState("Your transaction is empty","Choose a service or retail item to begin.","\uE7BF"));
  }

  foreach(var line in _vm.Lines)
  {
   var calc=FinancialEngine.CalculateLine(line);
   var cardContent=new StackPanel();

   var headRow=new Grid();
   headRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
   headRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
   headRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});

   var nameText=Ui.Text(line.Name,15,"#344052",true);
   var priceText=Ui.Text(Ui.Money(calc.Total),16,"#A85E67",true);
   priceText.Margin=new Thickness(8,0,8,0);
   priceText.VerticalAlignment=VerticalAlignment.Center;

   var delBtn=new Button{Content="×",Foreground=Ui.Brush("#C84D58"),Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(4,2,4,2),Cursor=Cursors.Hand,ToolTip="Remove item",VerticalAlignment=VerticalAlignment.Center,MinHeight=28,Height=28,Width=28};
   System.Windows.Automation.AutomationProperties.SetName(delBtn,"Remove "+line.Name);
   delBtn.Click+=(_,_)=>{_vm.Lines.Remove(line);RefreshCart();LoadCatalog();Ui.Toast($"Removed {line.Name}","info");};

   Grid.SetColumn(nameText,0);headRow.Children.Add(nameText);
   Grid.SetColumn(priceText,1);headRow.Children.Add(priceText);
   Grid.SetColumn(delBtn,2);headRow.Children.Add(delBtn);
   cardContent.Children.Add(headRow);

   var details=$"{Ui.Money(line.UnitPrice)} each";
   if(line.Charges.Count>0)details+=$" · +{line.Charges.Count} charge(s)";
   if(line.DiscountPercent>0)details+=$" · {line.DiscountPercent}% off";
   var detailsText=Ui.Text(details,12,"#77727B");
   detailsText.Margin=new Thickness(0,3,0,8);
   cardContent.Children.Add(detailsText);

   var actionRow=new Grid();
   actionRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
   actionRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});

   var stepper=new StackPanel{Orientation=Orientation.Horizontal};
   var minusBtn=new Button{Content="−",Width=30,Height=30,MinHeight=30,Padding=new Thickness(0),Margin=new Thickness(0,0,4,0),ToolTip="Decrease",Cursor=Cursors.Hand};
   System.Windows.Automation.AutomationProperties.SetName(minusBtn,"Decrease "+line.Name+" quantity");
   minusBtn.Click+=(_,_)=>{if(line.Quantity>1){line.Quantity--;}else{_vm.Lines.Remove(line);}RefreshCart();LoadCatalog();};
   var qtyBadge=new Border{Background=Ui.Brush("#F3E7E7"),CornerRadius=new CornerRadius(6),MinWidth=34,Height=30,Margin=new Thickness(0,0,4,0),Padding=new Thickness(5,0,5,0),Child=new TextBlock{Text=$"{line.Quantity:0.######}",FontWeight=FontWeights.Bold,Foreground=Ui.Brush("#A85E67"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
   var plusBtn=new Button{Content="+",Width=30,Height=30,MinHeight=30,Padding=new Thickness(0),ToolTip="Increase",Cursor=Cursors.Hand};
   System.Windows.Automation.AutomationProperties.SetName(plusBtn,"Increase "+line.Name+" quantity");
   plusBtn.Click+=(_,_)=>{line.Quantity++;RefreshCart();LoadCatalog();};
   stepper.Children.Add(minusBtn);stepper.Children.Add(qtyBadge);stepper.Children.Add(plusBtn);

   var editBtn=Ui.Button("Details & Pricing",()=>Customize(line,false));
   editBtn.HorizontalAlignment=HorizontalAlignment.Right;
   editBtn.Margin=new Thickness(0);

   Grid.SetColumn(stepper,0);actionRow.Children.Add(stepper);
   Grid.SetColumn(editBtn,1);actionRow.Children.Add(editBtn);
   cardContent.Children.Add(actionRow);

   var cartCard=Ui.Card(cardContent,new Thickness(0,0,0,10));cartCard.Padding=new Thickness(12);_cart.Children.Add(cartCard);
  }
  UpdateTotal();
 }

 private OrderCalculation Calculate()=>FinancialEngine.CalculateOrder(_vm.Lines,Ui.Number(_orderDiscount,"discount percentage"));

 private void UpdateTotal()
 {
  try
  {
   _error.Text="";
   _checkout.IsEnabled=_vm.Lines.Count>0;
   if(_vm.Lines.Count==0){_total.Text=Ui.Money(0);_subtotalValue.Text=Ui.Money(0);_discountValue.Text=Ui.Money(0);return;}
   var order=Calculate();
   _subtotalValue.Text=Ui.Money(order.Subtotal);
   _discountValue.Text=order.DiscountAmount>0?"− "+Ui.Money(order.DiscountAmount):Ui.Money(0);
   _total.Text=Ui.Money(order.Total);
  }
  catch(Exception ex){_error.Text=ex.Message;_total.Text="Check transaction";_checkout.IsEnabled=false;}
 }

 private void Customize(CartLineInput original,bool adding)
 {
  var copy=new CartLineInput{CatalogItemId=original.CatalogItemId,Name=original.Name,Kind=original.Kind,UnitPrice=original.UnitPrice,Quantity=original.Quantity,DiscountPercent=original.DiscountPercent,EmployeeId=original.EmployeeId,PriceRuleId=original.PriceRuleId,Charges=original.Charges.Select(c=>new AdditionalChargeInput{Name=c.Name,Description=c.Description,Amount=c.Amount}).ToList(),AdditionalMaterialQuantities=original.AdditionalMaterialQuantities.ToDictionary()};
  var price=Ui.CurrencyInput(copy.UnitPrice,double.NaN);
  var quantity=Ui.Input(copy.Quantity.ToString("0.######"));
  var discount=Ui.PercentageInput(copy.DiscountPercent,double.NaN);
  var selectedService=copy.Kind==SaleItemKind.Service?_business.List<SalonService>(_user).FirstOrDefault(s=>s.Id==copy.CatalogItemId):null;
  price.IsReadOnly=!_user.AllowCustomPrices && selectedService?.PricingType is not (PricingType.Range or PricingType.StartsAt);

  var staff=new ComboBox{DisplayMemberPath="FullName"};
  var employees=new List<Employee>{new(){Id=Guid.Empty,FullName="Unassigned"}};
  employees.AddRange(_business.List<Employee>(_user).Where(e=>e.IsActive));
  staff.ItemsSource=employees;
  staff.SelectedItem=employees.FirstOrDefault(e=>e.Id==copy.EmployeeId)??employees[0];

  var chargesPanel=new StackPanel();
  var final=Ui.Text("",24,"#A85E67",true);
  var footerTotal=Ui.Text("",22,"#A85E67",true);
  var err=Ui.Text("",12,"#C84D58");
  var baseValue=Ui.Text("",13,"#344052",true);
  var chargesValue=Ui.Text("",13,"#344052",true);
  var subtotalValue=Ui.Text("",13,"#344052",true);
  var discountValue=Ui.Text("",13,"#27845F",true);

  void Update()
  {
   try
   {
    copy.UnitPrice=Ui.Number(price,"price");
    copy.Quantity=Ui.Number(quantity,"quantity");
    copy.DiscountPercent=Ui.Number(discount,"discount");
    copy.EmployeeId=(staff.SelectedItem as Employee)?.Id is Guid id&&id!=Guid.Empty?id:null;
    var calculation=FinancialEngine.CalculateLine(copy);
    if(copy.DiscountPercent>_user.MaxDiscountPercent)throw new InvalidOperationException($"Your discount limit is {_user.MaxDiscountPercent}%.");
    baseValue.Text=Ui.Money(calculation.BaseAmount);
    chargesValue.Text=Ui.Money(calculation.ChargeAmount);
    subtotalValue.Text=Ui.Money(calculation.Subtotal);
    discountValue.Text="− "+Ui.Money(calculation.DiscountAmount);
    final.Text=Ui.Money(calculation.Total);
    footerTotal.Text=final.Text;
    err.Text="";
   }
   catch(Exception ex){err.Text=ex.Message;final.Text="Check pricing";footerTotal.Text=final.Text;baseValue.Text="—";chargesValue.Text="—";subtotalValue.Text="—";discountValue.Text="—";}
  }

  Action refresh=()=>{};

  void ChargeEditor(AdditionalChargeInput? current)
  {
   var name=Ui.Input(current?.Name??"");
   var desc=Ui.Input(current?.Description??"");
   var amount=Ui.CurrencyInput(current?.Amount??0,double.NaN);
   var body=Ui.Stack(Ui.Field("Charge name",name),Ui.Field("Description",desc),Ui.Field("Amount per unit (₱)",amount));
   var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
   var dlg=Ui.Dialog("Additional charge",body,490,490,footer);
   footer.Children.Add(Ui.Button("Cancel",()=>dlg.Close()));
   footer.Children.Add(Ui.Button("Save charge",()=>{var value=Ui.Number(amount,"charge amount");FinancialEngine.ValidateMoney(value);if(string.IsNullOrWhiteSpace(name.Text))throw new InvalidOperationException("Enter a charge name.");if(current==null)copy.Charges.Add(new(){Name=name.Text.Trim(),Description=desc.Text,Amount=value});else{current.Name=name.Text.Trim();current.Description=desc.Text;current.Amount=value;}refresh();dlg.Close();},true));
   dlg.ShowDialog();
  }

  refresh=()=>{
   chargesPanel.Children.Clear();
   if(copy.Charges.Count==0)
   {
    chargesPanel.Children.Add(Ui.Text("No additional charges applied.",13,"#77727B"));
   }
   else
   {
    foreach(var c in copy.Charges)
    {
     var row=new Grid{Margin=new Thickness(0,4,0,4)};
     row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
     row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
     row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
     var label=Ui.Stack(Ui.Text(c.Name+"  ·  "+Ui.Money(c.Amount)+" / unit",13,"#344052",true));
     if(!string.IsNullOrWhiteSpace(c.Description))label.Children.Add(Ui.Text(c.Description,12,"#77727B"));
     label.VerticalAlignment=VerticalAlignment.Center;label.Margin=new Thickness(0,0,12,0);row.Children.Add(label);
     var edit=Ui.Button("Edit",()=>ChargeEditor(c));Grid.SetColumn(edit,1);row.Children.Add(edit);
     var remove=Ui.IconButton("\uE711","Remove charge",()=>{copy.Charges.Remove(c);refresh();});Grid.SetColumn(remove,2);row.Children.Add(remove);
     chargesPanel.Children.Add(row);
    }
   }
   Update();
  };

  // Header
  var headerPanel=new StackPanel{Margin=new Thickness(0,0,0,14)};
  headerPanel.Children.Add(Ui.Badge(copy.Kind==SaleItemKind.Service?"SERVICE":copy.Kind==SaleItemKind.Product?"RETAIL PRODUCT":"PREPAID PACKAGE","#F3E7E7","#A85E67"));
  var itemTitle=Ui.Text(copy.Name,24,"#344052",true);itemTitle.Margin=new Thickness(0,7,0,4);headerPanel.Children.Add(itemTitle);
  headerPanel.Children.Add(Ui.Text($"Base unit price {Ui.Money(original.UnitPrice)} · Charges apply to each unit.",13,"#77727B"));

  // Main details card
  var detailsStack=new StackPanel();
  detailsStack.Children.Add(Ui.Text("Pricing & Quantity",15,"#344052",true));
  var numGrid=new Grid{Margin=new Thickness(0,8,0,0)};
  numGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  numGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(16)});
  numGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  var priceField=Ui.Field("Agreed unit price (₱)",price);
  var qtyField=Ui.Field("Quantity",quantity);
  Grid.SetColumn(priceField,0);
  Grid.SetColumn(qtyField,2);
  numGrid.Children.Add(priceField);
  numGrid.Children.Add(qtyField);
  detailsStack.Children.Add(numGrid);
  detailsStack.Children.Add(Ui.Field("Assigned staff",staff));

  if(copy.Kind==SaleItemKind.Service)
  {
   var rules=_business.ListPriceRules(_user,copy.CatalogItemId!.Value).Where(r=>!r.RequiresAuthorization||_user.AllowCustomPrices).ToList();
   if(rules.Count>0)
   {
    var rulesPicker=new ComboBox{ItemsSource=rules,DisplayMemberPath="Name",SelectedItem=rules.FirstOrDefault(r=>r.Id==copy.PriceRuleId)};
    detailsStack.Children.Add(Ui.Field("Saved price rule",rulesPicker));
    rulesPicker.SelectionChanged+=(_,_)=>{if(rulesPicker.SelectedItem is ServicePriceRule rule){copy.PriceRuleId=rule.Id;price.IsReadOnly=false;if(rule.MinimumPrice.HasValue)price.Text=rule.MinimumPrice.Value.ToString("0.00");}};
   }
  }

  // Discount card
  var discountStack=new StackPanel();
  discountStack.Children.Add(Ui.Text("Discount",15,"#344052",true));
  discountStack.Children.Add(Ui.Field("Item discount (%)",discount));
  var discountPills=new WrapPanel{Margin=new Thickness(0,0,0,8)};
  foreach(var p in new[]{0,5,10,15,20})
  {
   var btn=Ui.Pill(p+"%",()=>discount.Text=p.ToString());
   btn.Margin=new Thickness(0,0,5,5);btn.Padding=new Thickness(9,5,9,5);btn.IsEnabled=p<=_user.MaxDiscountPercent;
   discountPills.Children.Add(btn);
  }
  discountStack.Children.Add(discountPills);
  discountStack.Children.Add(Ui.Text($"Your authorized limit is {_user.MaxDiscountPercent}%. The amount below updates immediately.",11,"#77727B"));

  // Charges card
  var chargesCard=new StackPanel();
  if(copy.Kind==SaleItemKind.Service)
  {
   var chHeader=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,10)};
   chHeader.Children.Add(Ui.Text("Additional Charges",15,"#344052",true));
   var addCharge=Ui.Button("+ Add charge",()=>ChargeEditor(null));
   addCharge.IsEnabled=_user.AllowCustomPrices;
   addCharge.Margin=new Thickness(16,0,0,0);
   addCharge.Padding=new Thickness(10,4,10,4);
   addCharge.ToolTip=_user.AllowCustomPrices?"Add any authorized custom charge":"Owner permission is required for custom charges. Choose an available preset.";
   chHeader.Children.Add(addCharge);
   chargesCard.Children.Add(chHeader);
   chargesCard.Children.Add(chargesPanel);

   var presets=_business.List<ServiceChargePreset>(_user).Where(p=>p.IsActive&&(p.ServiceId==null||p.ServiceId==copy.CatalogItemId)).ToList();
   if(presets.Count>0)
   {
    var presetRow=new Grid{Margin=new Thickness(0,10,0,0)};
    presetRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
    presetRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
    var picker=new ComboBox{ItemsSource=presets,DisplayMemberPath="Name",Margin=new Thickness(0,0,8,0)};
    var addPreset=Ui.Button("Add preset",()=>{if(picker.SelectedItem is ServiceChargePreset p){copy.Charges.Add(new(){Name=p.Name,Description=p.Description,Amount=p.Amount});refresh();}});
    Grid.SetColumn(picker,0);
    Grid.SetColumn(addPreset,1);
    presetRow.Children.Add(picker);
    presetRow.Children.Add(addPreset);
    chargesCard.Children.Add(Ui.Field("Add saved charge preset",presetRow));
   }
  }

  // Materials card
  var extraFields=new List<(Guid ProductId,TextBox Field)>();
  var materialsCard=new StackPanel();
  if(copy.Kind==SaleItemKind.Service)
  {
   var materials=_business.List<ServiceMaterialRequirement>(_user).Where(m=>m.ServiceId==copy.CatalogItemId).ToList();
   var products=_business.ListMaterialProducts(_user).ToDictionary(p=>p.Id);
   if(materials.Count>0)
   {
    materialsCard.Children.Add(Ui.Text("Extra Consumables Used",15,"#344052",true));
    materialsCard.Children.Add(Ui.Text("Materials used for this entire line (in addition to configured recipe quantities).",12,"#77727B"));
    foreach(var m in materials)
    {
     var field=Ui.Input(copy.AdditionalMaterialQuantities.GetValueOrDefault(m.ProductId).ToString("0.######"));
     materialsCard.Children.Add(Ui.Field(products.GetValueOrDefault(m.ProductId)?.Name??"Material",field));
     extraFields.Add((m.ProductId,field));
    }
   }
  }

  var summaryCard=new Border{Background=Ui.Brush("#F3E7E7"),BorderBrush=Ui.Brush("#E9DADB"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(18,14,18,14),Margin=new Thickness(0,0,0,14)};
  var summaryStack=new StackPanel();
  summaryStack.Children.Add(SummaryRow("Base price × quantity",baseValue));
  summaryStack.Children.Add(SummaryRow("Additional charges × quantity",chargesValue));
  summaryStack.Children.Add(SummaryRow("Subtotal",subtotalValue));
  summaryStack.Children.Add(SummaryRow("Item discount",discountValue));
  summaryStack.Children.Add(new Border{Height=1,Background=Ui.Brush("#E9DADB"),Margin=new Thickness(0,8,0,8)});
  summaryStack.Children.Add(SummaryRow("Final total",final));
  summaryStack.Children.Add(err);
  summaryCard.Child=summaryStack;

  var mainContent=new StackPanel();
  mainContent.Children.Add(headerPanel);
  var pricingGrid=new Grid();pricingGrid.ColumnDefinitions.Add(new(){Width=new GridLength(1.2,GridUnitType.Star)});pricingGrid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  var detailsCard=Ui.Card(detailsStack,new Thickness(0,0,12,12));detailsCard.Padding=new Thickness(16);pricingGrid.Children.Add(detailsCard);
  var discountCard=Ui.Card(discountStack,new Thickness(0,0,0,12));discountCard.Padding=new Thickness(16);Grid.SetColumn(discountCard,1);pricingGrid.Children.Add(discountCard);
  mainContent.Children.Add(pricingGrid);
  if(copy.Kind==SaleItemKind.Service) mainContent.Children.Add(Ui.Card(chargesCard));
  if(materialsCard.Children.Count>0) mainContent.Children.Add(Ui.Card(materialsCard));
  mainContent.Children.Add(summaryCard);

  var footer=new Grid();footer.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  footer.Children.Add(Ui.Stack(Ui.Text("Final total",12,"#77727B"),footerTotal));
  var dialog=Ui.Dialog(copy.Kind switch{SaleItemKind.Service=>"Service Details & Pricing",SaleItemKind.Product=>"Product Details & Pricing",_=>"Package Details & Pricing"},mainContent,780,840,footer);
  dialog.MinWidth=Math.Min(650,SystemParameters.WorkArea.Width-40);
  var actions=Ui.Row(Ui.Button(adding?copy.Kind switch{SaleItemKind.Service=>"Add Service",SaleItemKind.Product=>"Add Product",_=>"Add Package"}:"Save changes",()=>{
   Update();
   if(err.Text.Length>0)throw new InvalidOperationException(err.Text);
   foreach(var extra in extraFields)
   {
    var value=Ui.Number(extra.Field,"extra material quantity");
    if(value<0 || decimal.Round(value,6)!=value)throw new InvalidOperationException("Extra material quantity must be nonnegative with at most six decimals.");
    if(value==0)copy.AdditionalMaterialQuantities.Remove(extra.ProductId);else copy.AdditionalMaterialQuantities[extra.ProductId]=value;
   }
   FinancialEngine.CalculateLine(copy);
   if(!adding)_vm.Lines.Remove(original);
   _vm.Lines.Add(copy);
   RefreshCart();
   LoadCatalog();
   dialog.Close();
   Ui.Toast(adding?$"Added {copy.Name} to the transaction":"Updated item");
  },true),Ui.Button("Cancel",()=>dialog.Close()));actions.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(actions,1);footer.Children.Add(actions);

  price.TextChanged+=(_,_)=>Update();
  quantity.TextChanged+=(_,_)=>Update();
  discount.TextChanged+=(_,_)=>Update();
  refresh();
  dialog.ShowDialog();
 }

 private void Checkout()
 {
  var order=Calculate();
  if(order.DiscountPercent>_user.MaxDiscountPercent)throw new InvalidOperationException($"Your discount limit is {_user.MaxDiscountPercent}%.");

  var payments=new List<PaymentInput>();
  var split=false;
  var selectedMethod=PaymentMethod.Cash;
  var processing=false;
  var list=new StackPanel{Margin=new Thickness(0,8,0,0)};
  var methodButtons=new Dictionary<PaymentMethod,Button>();
  var methodCards=new UniformGrid{Columns=5,Margin=new Thickness(0,12,0,14)};
  var methodHint=Ui.Text("Cash payment",13,"#77727B");
  var amount=Ui.CurrencyInput(order.Total,double.NaN);
  var reference=Ui.Input();
  System.Windows.Automation.AutomationProperties.SetName(amount,"Payment amount");
  System.Windows.Automation.AutomationProperties.SetName(reference,"Payment reference");
  var referenceField=Ui.Field("Payment reference",reference);
  var amountField=Ui.Field("Amount received (₱)",amount);
  var receivedValue=Ui.Text("",15,"#344052",true);
  var statusCard=new Border{CornerRadius=new CornerRadius(10),Padding=new Thickness(14),Margin=new Thickness(0,10,0,8)};
  var status=Ui.Text("",16,"#A85E67",true);
  statusCard.Child=status;
  var error=Ui.Text("",12,"#C84D58");
  var splitToggle=new Button();
  var complete=new Button{Content="Complete Sale",Style=(Style)FindResource("PrimaryButton"),Margin=new Thickness(0,12,0,6),MinHeight=46,IsDefault=true};
  Button? addPayment=null;
  var quickCashPills=new WrapPanel{Margin=new Thickness(0,4,0,10)};

  static string MethodName(PaymentMethod method)=>method switch{PaymentMethod.BankTransfer=>"Bank transfer",_=>method.ToString()};

  List<PaymentInput> CurrentPayments(bool validate)
  {
   if(split)return payments.ToList();
   if(order.Total==0)return [];
   var value=Ui.Number(amount,"payment amount");
   FinancialEngine.ValidateMoney(value);
   if(value==0&&!validate)return [];
   return [new(){Method=selectedMethod,Amount=value,Reference=reference.Text.Trim()}];
  }

  void PopulateQuickCash()
  {
   quickCashPills.Children.Clear();
   var exact=split?FinancialEngine.CalculatePayment(order.Total,payments,false).BalanceDue:order.Total;
   var suggestions=new List<decimal>{exact};
   foreach(var step in new decimal[]{100,500,1000})
   {
    var next=Math.Ceiling(exact/step)*step;
    if(next>exact&&!suggestions.Contains(next))suggestions.Add(next);
   }
   foreach(var denomination in new decimal[]{500,1000,2000})
    if(denomination>exact&&!suggestions.Contains(denomination))suggestions.Add(denomination);
   foreach(var value in suggestions.Where(s=>s>0).Take(4))
   {
    var pill=Ui.Pill(value==exact?"Exact "+Ui.Money(value):Ui.Money(value),()=>amount.Text=value.ToString("0.00"));
    pill.Margin=new Thickness(0,0,6,6);pill.Padding=new Thickness(10,5,10,5);quickCashPills.Children.Add(pill);
   }
  }

  void Refresh()
  {
   list.Children.Clear();
   foreach(var payment in payments)
   {
    var row=new Grid{Margin=new Thickness(0,0,0,6)};
    row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
    row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    var label=Ui.Stack(Ui.Text(MethodName(payment.Method),13,"#344052",true));
    if(payment.Reference.Length>0)label.Children.Add(Ui.Text(payment.Reference,11,"#77727B"));
    row.Children.Add(label);
    var value=Ui.Text(Ui.Money(payment.Amount),14,"#344052",true);value.Margin=new Thickness(10,0,10,0);value.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(value,1);row.Children.Add(value);
    var remove=Ui.IconButton("\uE711","Remove payment",()=>{payments.Remove(payment);amount.Text=FinancialEngine.CalculatePayment(order.Total,payments,false).BalanceDue.ToString("0.00");PopulateQuickCash();Refresh();});
    Grid.SetColumn(remove,2);row.Children.Add(remove);
    var entry=Ui.Card(row,new Thickness(0,0,0,5));entry.Padding=new Thickness(12,8,12,8);list.Children.Add(entry);
   }
   list.Visibility=split?Visibility.Visible:Visibility.Collapsed;
   if(addPayment!=null)addPayment.Visibility=split?Visibility.Visible:Visibility.Collapsed;
   referenceField.Visibility=selectedMethod==PaymentMethod.Cash?Visibility.Collapsed:Visibility.Visible;
   quickCashPills.Visibility=selectedMethod==PaymentMethod.Cash?Visibility.Visible:Visibility.Collapsed;
   ((TextBlock)((StackPanel)amountField).Children[0]).Text=split?"Amount for this payment (₱)":"Amount received (₱)";
   methodHint.Text=split?"Split payment · Select a method, enter the amount, then add each payment.":MethodName(selectedMethod)+" payment";
   foreach(var pair in methodButtons)
   {
    pair.Value.Background=pair.Key==selectedMethod?Ui.Brush("#F3E7E7"):Brushes.White;
    pair.Value.BorderBrush=pair.Key==selectedMethod?Ui.Brush("#C8797D"):Ui.Brush("#E9DADB");
    pair.Value.BorderThickness=new Thickness(pair.Key==selectedMethod?1.5:1);
   }
   splitToggle.Background=split?Ui.Brush("#F3E7E7"):Brushes.White;
   splitToggle.BorderBrush=split?Ui.Brush("#C8797D"):Ui.Brush("#E9DADB");
   try
   {
    var result=FinancialEngine.CalculatePayment(order.Total,CurrentPayments(false),false);
    receivedValue.Text=Ui.Money(result.AmountReceived);
    if(result.BalanceDue>0)
    {
     status.Text="Remaining balance  "+Ui.Money(result.BalanceDue);
     status.Foreground=Ui.Brush("#C88B35");statusCard.Background=Ui.Brush("#FBF3E7");
    }
    else if(result.Change>0)
    {
     status.Text="Change to customer  "+Ui.Money(result.Change);
     status.Foreground=Ui.Brush("#27845F");statusCard.Background=Ui.Brush("#EAF5F0");
    }
    else
    {
     status.Text=order.Total==0?"No payment required":"Payment covers the amount due";
     status.Foreground=Ui.Brush("#27845F");statusCard.Background=Ui.Brush("#EAF5F0");
    }
    error.Text="";
    var hasPendingAmount=split&&(!decimal.TryParse(amount.Text,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.CurrentCulture,out var pendingAmount)||pendingAmount!=0);
    if(result.BalanceDue==0&&hasPendingAmount)error.Text="Add the entered payment amount, or set it to zero to use the payments listed above.";
    complete.IsEnabled=!processing&&result.BalanceDue==0&&!hasPendingAmount;
   }
   catch(Exception ex)
   {
    error.Text=ex.Message;receivedValue.Text="—";status.Text="Check the payment amount";
    status.Foreground=Ui.Brush("#C84D58");statusCard.Background=Ui.Brush("#FBECEE");complete.IsEnabled=false;
   }
  }

  Button PaymentCard(string glyph,string label,Action action)
  {
   var icon=Ui.Text(glyph,21,"#A85E67");icon.FontFamily=new FontFamily("Segoe MDL2 Assets");icon.HorizontalAlignment=HorizontalAlignment.Center;
   var title=Ui.Text(label,12,"#344052",true);title.TextAlignment=TextAlignment.Center;title.Margin=new Thickness(0,8,0,0);
   var button=Ui.Button("",action);button.Content=Ui.Stack(icon,title);button.Margin=new Thickness(0,0,7,0);
   System.Windows.Automation.AutomationProperties.SetName(button,label+" payment");
   button.Padding=new Thickness(8,12,8,12);button.MinHeight=82;button.ToolTip=label;return button;
  }
  foreach(var (paymentMethod,label,glyph) in new[]{(PaymentMethod.Cash,"Cash","\uE8C7"),(PaymentMethod.GCash,"GCash","\uE8EA"),(PaymentMethod.BankTransfer,"Bank transfer","\uE825"),(PaymentMethod.Card,"Card","\uE8C7")})
  {
   var method=paymentMethod;
   var card=PaymentCard(glyph,label,()=>{selectedMethod=method;reference.Clear();Refresh();});
   methodButtons.Add(method,card);methodCards.Children.Add(card);
  }
  splitToggle=PaymentCard("\uE8AB","Split",()=>{
   if(split&&payments.Count>0&&!Ui.Confirm("Switch payment mode?","The entered split payments will be cleared.","Switch"))return;
   split=!split;payments.Clear();amount.Text=order.Total.ToString("0.00");reference.Clear();PopulateQuickCash();Refresh();
  });
  methodCards.Children.Add(splitToggle);

  var dueValue=Ui.Text(Ui.Money(order.Total),32,"#A85E67",true);
  var due=Ui.Stack(Ui.Text("AMOUNT DUE",11,"#77727B",true),dueValue,Ui.Text($"{order.Lines.Count} transaction line{(order.Lines.Count==1?"":"s")} · {(_customer.SelectedItem as Customer)?.FullName??"Walk-in customer"}",12,"#77727B"));
  var dueCard=Ui.Card(due);dueCard.Background=Ui.Brush("#FAF6F5");dueCard.Padding=new Thickness(18);
  var body=Ui.Stack(Ui.Text("Review the amount and select how your customer will pay.",13,"#77727B"),methodCards,dueCard,methodHint,amountField,quickCashPills,referenceField);
  var footer=new Grid();footer.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  footer.Children.Add(Ui.Stack(Ui.Text("Total due",12,"#77727B"),Ui.Text(Ui.Money(order.Total),24,"#A85E67",true)));
  var dialog=Ui.Dialog("Payment",body,740,780,footer);
  dialog.MinWidth=Math.Min(650,SystemParameters.WorkArea.Width-40);
  dialog.Closing+=(_,e)=>{if(processing)e.Cancel=true;};

  addPayment=Ui.Button("+ Add this payment",()=>{
   var value=Ui.Number(amount,"payment");FinancialEngine.ValidateMoney(value);
   if(value<=0)throw new InvalidOperationException("Enter a payment greater than zero.");
   var payment=new PaymentInput{Method=selectedMethod,Amount=value,Reference=reference.Text.Trim()};
   var result=FinancialEngine.CalculatePayment(order.Total,payments.Append(payment),false);
   payments.Add(payment);amount.Text=result.BalanceDue.ToString("0.00");reference.Clear();PopulateQuickCash();Refresh();
  });
  addPayment.Margin=new Thickness(0,0,0,6);
  body.Children.Add(addPayment);body.Children.Add(list);
  body.Children.Add(SummaryRow("Amount received",receivedValue));body.Children.Add(statusCard);body.Children.Add(error);

  complete.Click+=async (_,_)=>
  {
   if(processing)return;
   Sale savedSale;
   try
   {
    var finalPayments=CurrentPayments(true);
    FinancialEngine.CalculatePayment(order.Total,finalPayments);
    var selected=(_customer.SelectedItem as Customer)?.Id;
    var request=new CheckoutRequest{CheckoutToken=_vm.CheckoutToken,CustomerId=selected==Guid.Empty?null:selected,Lines=_vm.Lines.ToList(),Payments=finalPayments,TransactionDiscountPercent=order.DiscountPercent,DiscountReason=_discountReason.Text.Trim()};
    processing=true;body.IsEnabled=false;footer.IsEnabled=false;complete.IsEnabled=false;complete.Content="Saving payment…";error.Text="";
    savedSale=await Task.Run(()=>_business.Checkout(request,_user));
   }
   catch(Exception ex)
   {
    processing=false;body.IsEnabled=true;footer.IsEnabled=true;complete.Content="Complete Sale";
    error.Text=ex.GetBaseException().Message;complete.IsEnabled=true;return;
   }
   processing=false;
   _vm.NewOrder();_orderDiscount.Text="0";_discountReason.Clear();
   dialog.Close();
   Ui.Toast($"Sale {savedSale.TransactionNumber} completed successfully");
   Ui.Guard(()=>{RefreshCart();RefreshCatalog();_onComplete(savedSale);});
  };
  var paymentActions=Ui.Row();paymentActions.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(paymentActions,1);footer.Children.Add(paymentActions);
  var back=Ui.Button("Back",()=>dialog.Close());back.ToolTip="Return to the current transaction";back.Margin=new Thickness(0,0,12,0);back.MinWidth=84;back.MinHeight=40;back.Padding=new Thickness(12,8,12,8);paymentActions.Children.Add(back);
  complete.Margin=new Thickness(0);complete.MinHeight=40;complete.MinWidth=140;complete.Padding=new Thickness(12,8,12,8);paymentActions.Children.Add(complete);
  amount.TextChanged+=(_,_)=>Refresh();
  PopulateQuickCash();Refresh();dialog.ShowDialog();
 }

 internal CartLineInput SmokeDialogs()
 {
  var service=_services.Single(s=>s.Name=="Hair Color");
  var line=new CartLineInput{CatalogItemId=service.Id,Name=service.Name,Kind=SaleItemKind.Service,UnitPrice=500,Quantity=1,DiscountPercent=10,Charges=[new(){Name="Long Hair",Amount=300},new(){Name="Thick Hair",Amount=200},new(){Name="Extra Product",Amount=150}]};
  Customize(line,true);
  _vm.Lines.Add(line);
  RefreshCart();
  Checkout();
  _vm.NewOrder();
  RefreshCart();
  return line;
 }

 private void NewCustomer()
 {
  var name=Ui.Input();var phone=Ui.Input();var email=Ui.Input();
  var body=Ui.Stack(Ui.Field("Full name",name),Ui.Field("Phone",phone),Ui.Field("Email (optional)",email));
  var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var dialog=Ui.Dialog("New customer",body,510,460,footer);
  footer.Children.Add(Ui.Button("Cancel",()=>dialog.Close()));
  footer.Children.Add(Ui.Button("Save customer",()=>{var customer=_business.Save(new Customer{FullName=name.Text.Trim(),Phone=phone.Text.Trim(),Email=email.Text.Trim()},_user);RefreshCatalog();_customer.SelectedItem=(_customer.ItemsSource as IEnumerable<Customer>)?.First(c=>c.Id==customer.Id);dialog.Close();Ui.Toast($"Added customer {customer.FullName}");},true));
  dialog.ShowDialog();
 }
}
