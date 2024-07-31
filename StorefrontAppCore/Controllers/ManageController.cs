using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using StorefrontAppCore.Utilities;
using StorefrontAppCore.Models;
using StorefrontAppCore.Data;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace StorefrontAppCore.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly Cryptography _cryptography;
        private readonly ILogger<ManageController> _logger;

        public ManageController(ApplicationDbContext context, Cryptography cryptography, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<ManageController> logger)
        {
            _dbContext = context;
            _cryptography = cryptography;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            string userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int retVal))
            {
                return retVal;
            }

            return null;
        }

        private string? GetCurrentUsername()
        {
            return User?.FindFirstValue(ClaimTypes.Name);
        }

        private async Task<User> GetCurrentUserAsync(int? id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        private async Task<bool> HasPassword()
        {
            var user = await _userManager.FindByIdAsync(GetCurrentUserId().ToString());

            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            ChangeUsernameSuccess,
            ChangeEmailSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        public class TransactionRequest
        {
            public string EncryptedCardNumber { get; set; }
            public string EncryptedKeyPIN { get; set; }
            public List<VendorTransaction> VendorTransactions { get; set; }
        }

        public class VendorTransaction
        {
            public string VendorAlias { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class TransactionResponse
        {
            public List<string> Certificates { get; set; }
        }

        public class RefundRequest
        {
            public List<string> Certificates { get; set; }
            public List<decimal> Amounts { get; set; }
        }

        // GET: /Manage/Index
        public async Task<IActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.ChangeUsernameSuccess ? "Your username has been changed."
                : message == ManageMessageId.ChangeEmailSuccess ? "Your email has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }

            User user = await GetCurrentUserAsync(GetCurrentUserId());
            int? userId = GetCurrentUserId();    
            List<ShoppingCartItem> userShoppingCartItems = await _dbContext.GetShoppingCartItemsListAsync(userId);
            List<Order> userOrders = await _dbContext.GetOrdersListAsync(userId);
            int userStoreAccountCount = _dbContext.GetStoreAccountQuery(userId).Count();
            StoreAccount account = await _dbContext.GetStoreAccountAsync(userId);
            string accountAlias = account?.Alias;

            List<PaymentMethod> userPaymentMethods = await _dbContext.GetPaymentMethodListAsync(userId);

            bool storeAccountCreated = (userStoreAccountCount == 0) ? false : true;

            IndexViewModel model = new IndexViewModel
            {
                HasPassword = await HasPassword(),
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                EmailConfirmed = user.EmailConfirmed,
                StoreAccountCreated = storeAccountCreated,
                Orders = userOrders,
                AliasName = accountAlias,
                ShoppingCartItems = userShoppingCartItems,
                PaymentMethods = userPaymentMethods
            };
            return View(model);
        }

        // POST: /Manage/CreateStoreAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStoreAccount(AccountType accountTypeInput, string accountAliasInput)
        {
            int? userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error, please fill out the fields with valid data.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            StoreAccount userStoreAccount = new StoreAccount
            {
                HolderID = userId,
                DateOpened = DateTime.Now,
                Alias = accountAliasInput,
                AccountType = accountTypeInput,
            };

            try
            {
                _dbContext.StoreAccounts.Add(userStoreAccount);
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with creating the store account.";
                _logger.LogInformation("Success with creating the store account.");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the store account.";
                _logger.LogWarning($"Error adding the store account to the database. {ex}");
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        // POST: /Manage/SetAccountAlias
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAccountAlias(string ChangeAliasInput)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error, please fill out the fields with valid data.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            int? userId = GetCurrentUserId();
            StoreAccount userAccount = await _dbContext.GetStoreAccountAsync(userId);
            bool existingUser = await _dbContext.CheckExistingUserAsync(ChangeAliasInput);

            if (existingUser)
            {
                TempData["Message"] = "This alias is already in use. Use a different one.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            try
            {
                userAccount.Alias = ChangeAliasInput;
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with changing the account alias.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the store account.";
                _logger.LogWarning($"Error with creating the store account. {ex}");
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        // POST: /Manage/AddPaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPaymentMethod(string cardNumber, string keyPIN)
        {
            int? userId = GetCurrentUserId();

            // Manual validation for cardNumber
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 11)
            {
                TempData["Message"] = "The Card Number field must be a length of 11.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            // Manual validation for KeyPIN
            if (string.IsNullOrWhiteSpace(keyPIN))
            {
                TempData["Message"] = "The Key ID field is required and must be 5 digits.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            PaymentMethod userPaymentMethod = new PaymentMethod
            {
                AccountID = userId,
                CardNumber = cardNumber,
                KeyPIN = keyPIN,
                Deactivated = false
            };

            StoreAccount storeAccount = await _dbContext.GetStoreAccountAsync(userId);
            PaymentMethod duplicatePaymentMethod = await _dbContext.GetExistingPaymentMethodAsync(userId, cardNumber, keyPIN);
            string userAlias = storeAccount.Alias;

            if (duplicatePaymentMethod != null)
            {
                if (!duplicatePaymentMethod.Deactivated)
                {
                    TempData["Message"] = "Please use a payment method that isn't registered for you already.";
                    return RedirectToAction(nameof(ManageController.Index));
                }
                else
                {
                    // Reactivating the card, since it exists already, but was previously deactived due to removal under a reference with an order
                    duplicatePaymentMethod.Deactivated = false;
                    userPaymentMethod = duplicatePaymentMethod;
                }
            }

            // Encrypting the KeyPIN, card number, and alias
            string encryptedKeyPIN = _cryptography.EncryptValue(keyPIN);
            string encryptedCardNumber = _cryptography.EncryptValue(cardNumber);
            string encryptedAlias = _cryptography.EncryptValue(userAlias);

            // Calling the Bank API
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://slushbanking20240729194751.azurewebsites.net/"); // Bank app's URL
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                var response = await client.GetAsync($"api/bankAPI/VerifyCard?encryptedCardNumber={Uri.EscapeDataString(encryptedCardNumber)}&encryptedKeyPIN={Uri.EscapeDataString(encryptedKeyPIN)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Card validation failed.";
                    return RedirectToAction(nameof(ManageController.Index));
                }

                string result = await response.Content.ReadAsStringAsync();
                if (result == "false")
                {
                    TempData["Message"] = "Invalid card number or PIN.";
                    return RedirectToAction(nameof(ManageController.Index));
                }
            }

            try
            {
                _dbContext.PaymentMethods.Update(userPaymentMethod);
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with creating the payment method.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the payment method.";
                _logger.LogWarning($"Error with creating the payment method. {ex}");
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePaymentMethod(int paymentMethodID)
        {
            PaymentMethod userPaymentMethod = await _dbContext.GetPaymentMethodFromPKeyAsync(paymentMethodID);

            List<Order> relatedOrders = await _dbContext.GetRelatedOrdersListAsync(paymentMethodID);

            if (relatedOrders.Count != 0)
            {
                // Cascade deletion rules give constraints, so we cannot remove it without complex logic that also removes the Order(s) involved
                foreach (var order in relatedOrders)
                {
                    // Update the boolean flag to reflect its removal
                    order.PaymentMethod.Deactivated = true;
                }
            }
            else
            {
                _dbContext.PaymentMethods.Remove(userPaymentMethod);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with removing the payment method.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error with removing the payment method.";
                _logger.LogWarning($"Error with removing the payment method. {ex}");
                return View("CustomError");
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        // GET: /Manage/CreateOrder
        public async Task<ActionResult> CreateOrder()   // Used to populate the orders view
        {
            int? userId = GetCurrentUserId();

            List<ShoppingCartItem> shoppingCartItems = await _dbContext.GetShoppingCartItemsListAsync(userId);
            List<PaymentMethod> activePaymentMethods = await _dbContext.GetActivePaymentMethodsListAsync(userId);

            CreateOrderViewModel model = new CreateOrderViewModel
            {
                ShoppingCartItems = shoppingCartItems,
                PaymentMethods = activePaymentMethods
            };

            return View(model);
        }

        // POST: /Manage/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(CreateOrderViewModel model)
        {
            int? userId = GetCurrentUserId();
            ShoppingCart userCart = await _dbContext.GetShoppingCartAsync(userId);
            List<PaymentMethod> paymentMethods = await _dbContext.GetPaymentMethodListAsync(userId);
            model.ShoppingCartItems = userCart.ShoppingCartItems.ToList();
            model.PaymentMethods = paymentMethods;

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error with processing the order.";
                return View(model);
            }

            PaymentMethod paymentMethod = await _dbContext.GetPaymentMethodFromPKeyAsync(model.SelectedPaymentMethodID);

            string encryptedKeyPIN = _cryptography.EncryptValue(paymentMethod.KeyPIN);
            string encryptedCardNumber = _cryptography.EncryptValue(paymentMethod.CardNumber);

            decimal totalOrderPrice = 0;
            List<OrderItem> items = new List<OrderItem>();

            Order order = new Order
            {
                BuyerID = userId,
                PaymentMethodID = model.SelectedPaymentMethodID,
                PaymentMethod = paymentMethod,
                ShippingAddress = model.ShippingAddress,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in userCart.ShoppingCartItems)
            {
                decimal currentOrderPrice = item.Product.Price * item.Quantity;
                totalOrderPrice += currentOrderPrice;

                OrderItem orderItem = new OrderItem
                {
                    ProductID = item.Product.ProductID,
                    Quantity = item.Quantity,
                    TotalPrice = currentOrderPrice
                };

                items.Add(orderItem);
            }

            // Grouping the items by vendor alias
            var vendorGroups = userCart.ShoppingCartItems
                .GroupBy(item => item.Product.Supplier.Account.Alias)
                .Select(group => new
                {
                    VendorAlias = group.Key,
                    TotalAmount = group.Sum(item => item.Product.Price * item.Quantity)
                }).ToList();

            TransactionRequest transactionRequest = new TransactionRequest
            {
                EncryptedCardNumber = encryptedCardNumber,
                EncryptedKeyPIN = encryptedKeyPIN,
                VendorTransactions = vendorGroups.Select(vg => new VendorTransaction
                {
                    VendorAlias = _cryptography.EncryptValue(vg.VendorAlias),
                    TotalAmount = vg.TotalAmount
                }).ToList()
            };

            string userAlias = userCart.Account.Alias;
            string encryptedAlias = _cryptography.EncryptValue(userAlias);

            // Calling the Bank API
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://slushbanking20240729194751.azurewebsites.net/");
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                string jsonContent = JsonSerializer.Serialize(transactionRequest);
                var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/bankAPI/InitiateTransaction", contentString);

                if (!response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    if (content.Contains("Not enough funds to complete the purchase."))
                    {
                        ViewBag.Message = "Not enough money to complete the transaction.";
                        return View(model);
                    }

                    ViewBag.Message = "Card validation failed, there was a server error.";
                    return View(model);
                }

                var responseContent = await response.Content.ReadFromJsonAsync<TransactionResponse>();
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Certificate = responseContent.Certificates[i];
                }
            }

            order.OrderItems = items;
            order.TotalPrice = totalOrderPrice;
            order.PurchaseDate = DateTime.Now;
            order.Status = OrderStatus.Approved;

            try
            {
                _dbContext.Orders.Add(order);
                _dbContext.ShoppingCartsItems.RemoveRange(userCart.ShoppingCartItems);
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Order successfully completed.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Internal error with completing the order.";
                _logger.LogWarning($"Error with completing the order. {ex}");
                return View("CustomError");
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        // POST: /Manage/RefundOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundOrder(int orderID)
        {
            int? userId = GetCurrentUserId();
            Order order = await _dbContext.GetOrderAsync(orderID);
            var orderItems = order.OrderItems;
            string customerAlias = order.StoreAccount.Alias;

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error with processing the refund.";
                return RedirectToAction(nameof(ManageController.Index));
            }

            // Serializing the request for the payload
            RefundRequest refundRequest = new RefundRequest
            {
                Certificates = orderItems.Select(oi => oi.Certificate).ToList(),
                Amounts = orderItems.Select(oi => oi.TotalPrice).ToList()
            };

            string encryptedAlias = _cryptography.EncryptValue(customerAlias);

            // Calling the Bank API
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://slushbanking20240729194751.azurewebsites.net/");
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                string jsonContent = JsonSerializer.Serialize(refundRequest);
                var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/bankAPI/InitiateRefund", contentString);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Refund unsuccessful for order #{orderID}.");
                    TempData["Message"] = $"Failed to refund this order.";
                    return RedirectToAction(nameof(ManageController.Index));
                }

                _logger.LogWarning($"Refund success for order #{orderID}.");
                TempData["Message"] = $"Successfully completed the refund.";
                order.Status = OrderStatus.Refunded;

                try
                {
                    _dbContext.SaveChanges();
                    TempData["Message"] = "Refund successfully completed.";
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Internal error with completing the refund.";
                    _logger.LogWarning($"Error with refunding the order. {ex}");
                    return View("CustomError");
                }
            }

            return RedirectToAction(nameof(ManageController.Index));
        }

        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Error generating 2FA token.");
            }

            await _signInManager.RefreshSignInAsync(user);
            //await _emailService.SendEmailAsync(user.Email, "Your 2FA Code", $"Your 2FA code is {code}");

            return RedirectToAction("Index", "Manage");
        }

        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return BadRequest("2FA is not enabled for this user.");
            }

            var disable2FAResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2FAResult.Succeeded)
            {
                return BadRequest("Error disabling 2FA.");
            }

            _logger.LogInformation("User with ID '{UserId}' has disabled 2FA.", user.Id);

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", "Manage");
        }

        // GET: /Manage/ChangeEmail
        public IActionResult ChangeEmail()
        {
            return View();
        }

        // POST: /Manage/ChangeEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            User user = await GetCurrentUserAsync(GetCurrentUserId());

            if (user.Email.ToLowerInvariant() != model.OldEmail.ToLowerInvariant())   // ASP.NET Identity uses case-sensitivity with its fields for its DBContext and management
            {
                ModelState.AddModelError(string.Empty, "Old email doesn't match the current one.");
                return View(model);
            }

            user.Email = model.NewEmail;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(ManageController.Index), new { Message = ManageMessageId.ChangeEmailSuccess });
            }

            AddErrors(result);
            return View(model);
        }

        // GET: /Manage/ChangeUsername
        public IActionResult ChangeUsername()
        {
            return View();
        }

        // POST: /Manage/ChangeUsername
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            User user = await GetCurrentUserAsync(GetCurrentUserId());

            if (user.UserName != model.OldUsername)
            {
                ModelState.AddModelError(string.Empty, "Old username doesn't match the current one.");
                return View(model);
            }

            user.UserName = model.NewUsername;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(ManageController.Index), new { Message = ManageMessageId.ChangeUsernameSuccess });
            }

            AddErrors(result);
            return View(model);
        }

        // GET: /Manage/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = await GetCurrentUserAsync(GetCurrentUserId());

            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(ManageController.Index), new { Message = ManageMessageId.ChangePasswordSuccess });
                }

                AddErrors(result);
            }

            return View(model);
        }
    }
}