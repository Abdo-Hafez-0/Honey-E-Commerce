using Honey_E_commerce.Data;
using Honey_E_commerce.Models;
using Honey_E_commerce.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Honey_E_commerce.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext context;
        public ReviewsController(ApplicationDbContext _context)
        {
            context = _context;
        }
        public IActionResult Index()
        {
            return View("CheckReviews");
        }

        // GET: Review/Create (Main page)
        public IActionResult Create()
        {
            var model = new ReviewViewModel();

            // restore temp data from previous step
            if (TempData["CustomerName"] != null)
                model.CustomerName = TempData["CustomerName"].ToString();

            if (TempData["PhoneNumber"] != null)
                model.PhoneNumber = TempData["PhoneNumber"].ToString();

            if (TempData["Products"] != null)
                ViewBag.Products = TempData["Products"].ToString();

            if (TempData["ShowReviewForm"] != null)
                ViewBag.ShowReviewForm = true;

            return View("Reviews", model);
        }


        // POST: Review/LoadProducts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoadProducts(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ErrorMessage"] = "Phone number is required";
                return RedirectToAction("Create");
            }

            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "No customer found with this phone number";
                return View("CheckReviews");
            }

            var products = await context.Orders
                .Where(o => o.CustomerID == customer.ID)
                .Include(o => o.Product)
                .Select(o => new { o.ProductID, ProductName = o.Product.Name })
                .Distinct()
                .ToListAsync();

            if (!products.Any())
            {
                TempData["ErrorMessage"] = $"No products found for {customer.CustomerName}";
                return RedirectToAction("Create");
            }

            // Pass values back to page
            TempData["CustomerName"] = customer.CustomerName;
            TempData["PhoneNumber"] = phoneNumber;
            TempData["Products"] = System.Text.Json.JsonSerializer.Serialize(products);
            TempData["ShowReviewForm"] = true;

            return RedirectToAction("Create");
        }


        // POST: Review/SubmitReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(ReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields";
                TempData["CustomerName"] = model.CustomerName;
                TempData["PhoneNumber"] = model.PhoneNumber;
                TempData["ShowReviewForm"] = true;

                await ReloadProducts(model.PhoneNumber);
                return RedirectToAction("Create");
            }

            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == model.PhoneNumber);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found";
                return RedirectToAction("Create");
            }

            // verify purchase
            var hasPurchased = await context.Orders
                .AnyAsync(o => o.CustomerID == customer.ID && o.ProductID == model.ProductID);

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "You can only review products you purchased";
                TempData["CustomerName"] = model.CustomerName;
                TempData["PhoneNumber"] = model.PhoneNumber;
                TempData["ShowReviewForm"] = true;

                await ReloadProducts(model.PhoneNumber);
                return RedirectToAction("Create");
            }

            // check duplicate review
            var existingReview = await context.Reviews
                .FirstOrDefaultAsync(r => r.PhoneNumber == model.PhoneNumber &&
                                          r.ProductID == model.ProductID);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "You already reviewed this product";
                TempData["CustomerName"] = model.CustomerName;
                TempData["PhoneNumber"] = model.PhoneNumber;
                TempData["ShowReviewForm"] = true;

                await ReloadProducts(model.PhoneNumber);
                return RedirectToAction("Create");
            }

            // Save review
            var review = new Review
            {
                ReviewID = Guid.NewGuid(),
                ProductID = model.ProductID,
                PhoneNumber = model.PhoneNumber,
                Rating = model.Rating,
                Comment = model.ReviewText,
                ReviewDate = DateTime.Now
            };

            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thank you for your review, {customer.CustomerName}!";

            return RedirectToAction("Create");
        }


        // Helper function
        private async Task ReloadProducts(string phone)
        {
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phone);

            if (customer != null)
            {
                var products = await context.Orders
                    .Where(o => o.CustomerID == customer.ID)
                    .Include(o => o.Product)
                    .Select(o => new { o.ProductID, ProductName = o.Product.Name })
                    .Distinct()
                    .ToListAsync();

                TempData["Products"] = System.Text.Json.JsonSerializer.Serialize(products);
            }
        }
    }

}

