using Honey_E_commerce.Data;
using Honey_E_commerce.Models;
using Honey_E_commerce.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Honey_E_commerce.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext context;
        public OrderController(ApplicationDbContext _context)
        {
            context = _context;

        }
        public IActionResult Index()
        {
            decimal subtotal = 0m;
            var products = GetCartFromSession();
            Dictionary<Product, int> prdDic = new Dictionary<Product, int>();
            foreach (var item in products)
            {
                var prd = context.Products.Find(item.Key);
                prdDic.Add(prd, item.Value);
                subtotal += prd.Price * item.Value;
            }

            ViewBag.total = subtotal;

            return View("Order", prdDic);
        }

        public IActionResult Test(string fname)
        {
            return Content($"{fname}");
        }
        public IActionResult SubmitOrder(OrderDetails orderDetails)
        {

            var user = context.Customers.FirstOrDefault(c => c.PhoneNumber == orderDetails.PhoneNumber);

            Guid ID = user.ID;

            if(user == null)
            {
                var customerData = new Customer
                {
                    ID = Guid.NewGuid(),
                    CustomerName = orderDetails.FirstName + orderDetails.LastName,
                    Address = orderDetails.Address + orderDetails.City + orderDetails.State,
                    PhoneNumber = orderDetails.PhoneNumber
                };

                ID = customerData.ID;

                context.Customers.Add(customerData);
                context.SaveChanges();
            }



            var products = GetCartFromSession();

            foreach (var item in products)
            {
                var prd = context.Products.Single(x => x.ProductID == item.Key);
                var orderData = new Order
                {
                    OrderID = Guid.NewGuid(),
                    CustomerID = ID,
                    ProductID = item.Key,
                    Quantity = item.Value,
                    UnitPrice = (double)prd.Price
                };
                context.Orders.Add(orderData);
            }

            context.SaveChanges();

            HttpContext.Session.Remove("CartItems");

            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation()
        {
            return View();
        }
        private Dictionary<Guid, int> GetCartFromSession()
        {
            var cartData = HttpContext.Session.GetString("CartItems");
            if (string.IsNullOrEmpty(cartData))
            {
                return new Dictionary<Guid, int>();
            }
            // Format: "guid1:quantity1,guid2:quantity2"
            var cartItems = new Dictionary<Guid, int>();
            var items = cartData.Split(',');
            foreach (var item in items)
            {
                var parts = item.Split(':');
                if (parts.Length == 2 &&
                    Guid.TryParse(parts[0], out Guid productId) &&
                    int.TryParse(parts[1], out int quantity))
                {
                    cartItems[productId] = quantity;
                }
            }
            return cartItems;
        }
    }
}
