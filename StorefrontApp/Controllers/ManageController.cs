using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using BankingApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using StorefrontApp.Models;
using StorefrontApp.Utilities;

namespace StorefrontApp.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext _dbContext;
        private readonly Cryptography _cryptography;

        public ManageController()
            : this(new ApplicationDbContext(), null, null)
        {
        }

        public ManageController(ApplicationDbContext context, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            ContextDbManager = context;
            UserManager = userManager;
            SignInManager = signInManager;
            _cryptography = new Cryptography();
        }

        public ApplicationDbContext ContextDbManager
        {
            get
            {
                return _dbContext ?? HttpContext.GetOwinContext().Get<ApplicationDbContext>();
            }
            private set
            {
                _dbContext = value;
            }
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private int GetCurrentUserId()
        {
            return User.Identity.GetUserId<int>();
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

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.ChangeUsernameSuccess ? "Your username has been changed."
                : message == ManageMessageId.ChangeEmailSuccess ? "Your email has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            if (TempData.ContainsKey("Message"))
            {
                ViewBag.Message = TempData["Message"].ToString();
            }

            int userId = GetCurrentUserId();    
            User user = await UserManager.FindByIdAsync(userId);
            List<ShoppingCartItem> userShoppingCartItems = await _dbContext.GetShoppingCartItemsListAsync(userId);
            List<Order> userOrders = await _dbContext.GetOrdersListAsync(userId);
            int userStoreAccountCount = _dbContext.GetStoreAccountQuery(userId).Count();
            StoreAccount account = await _dbContext.GetStoreAccountAsync(userId);
            string accountAlias = account?.Alias;

            List<PaymentMethod> userPaymentMethods = await _dbContext.GetPaymentMethodListAsync(userId);

            bool storeAccountCreated = (userStoreAccountCount == 0) ? false : true;

            IndexViewModel model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(User.Identity.GetUserId()),
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
        public async Task<ActionResult> CreateStoreAccount(AccountType accountTypeInput, string accountAliasInput)
        {
            int userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error, please fill out the fields with valid data.";
                return RedirectToAction("Index");
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
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the store account.";
                Log.Warn($"Error adding the store account to the database. {ex}");
            }

            return RedirectToAction("Index");
        }

        // POST: /Manage/SetAccountAlias
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetAccountAlias(string ChangeAliasInput)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error, please fill out the fields with valid data.";
                return RedirectToAction("Index");
            }

            int userId = GetCurrentUserId();
            StoreAccount userAccount = await _dbContext.GetStoreAccountAsync(userId);
            bool existingUser = await _dbContext.CheckExistingUserAsync(ChangeAliasInput);

            if (existingUser)
            {
                TempData["Message"] = "This alias is already in use. Use a different one.";
                return RedirectToAction("Index");
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
                Log.Warn($"Error with creating the store account. {ex}");
            }

            return RedirectToAction("Index");
        }

        // POST: /Manage/AddPaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPaymentMethod(string cardNumber, string keyPIN)
        {
            int userId = GetCurrentUserId();

            // Manual validation for cardNumber.
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 11)
            {
                TempData["Message"] = "The Card Number field must be a length of 11.";
                return RedirectToAction("Index");
            }

            // Manual validation for KeyPIN.
            if (string.IsNullOrWhiteSpace(keyPIN))
            {
                TempData["Message"] = "The Key ID field is required and must be 5 digits.";
                return RedirectToAction("Index");
            }

            PaymentMethod userPaymentMethod = new PaymentMethod
            {
                AccountID = userId,
                CardNumber = cardNumber,
                KeyPIN = keyPIN,
                IsDeactivated = false
            };

            StoreAccount storeAccount = await _dbContext.GetStoreAccountAsync(userId);
            PaymentMethod duplicatePaymentMethod = await _dbContext.GetExistingPaymentMethodAsync(userId, cardNumber, keyPIN);
            string userAlias = storeAccount.Alias;

            if (duplicatePaymentMethod != null)
            {
                if (!duplicatePaymentMethod.IsDeactivated)
                {
                    TempData["Message"] = "Please use a payment method that isn't registered for you already.";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Reactivating the card, since it exists already, but was previously deactived due to removal under a reference with an order.
                    duplicatePaymentMethod.IsDeactivated = false;
                    userPaymentMethod = duplicatePaymentMethod;
                }
            }

            // Encrypting the KeyPIN, card number, and alias.
            string encryptedKeyPIN = _cryptography.EncryptValue(keyPIN);
            string encryptedCardNumber = _cryptography.EncryptValue(cardNumber);
            string encryptedAlias = _cryptography.EncryptValue(userAlias);

            // Calling the Bank API.
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44321/"); // Bank app's URL
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                var response = await client.GetAsync($"api/VerifyCard?encryptedCardNumber={Uri.EscapeDataString(encryptedCardNumber)}&encryptedKeyPIN={Uri.EscapeDataString(encryptedKeyPIN)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Card validation failed.";
                    return RedirectToAction("Index");
                }

                string result = await response.Content.ReadAsStringAsync();
                if (result.Contains("Card is not active") || result.Contains("Unauthorized"))
                {
                    TempData["Message"] = result;
                    return RedirectToAction("Index");
                }
            }

            try
            {
                _dbContext.PaymentMethods.AddOrUpdate(userPaymentMethod);
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with creating the payment method.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the payment method.";
                Log.Info($"Error with creating the payment method. {ex}");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePaymentMethod(int paymentMethodID)
        {
            PaymentMethod userPaymentMethod = await _dbContext.GetPaymentMethodFromPKeyAsync(paymentMethodID);

            List<Order> relatedOrders = await _dbContext.GetRelatedOrdersListAsync(paymentMethodID);

            if (relatedOrders.Any())
            {
                // Cascade deletion rules give constraints, so we cannot remove it without complex logic that also removes the Order(s) involved.
                foreach (var order in relatedOrders)
                {
                    // Update the boolean flag to reflect its removal.
                    order.PaymentMethod.IsDeactivated = true;
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
                Log.Warn($"Error with removing the payment method. {ex}");
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        // GET: /Manage/CreateOrder
        // Used to populate the orders view.
        public async Task<ActionResult> CreateOrder()
        {
            int userId = GetCurrentUserId();

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
        public async Task<ActionResult> CreateOrder(CreateOrderViewModel model)
        {
            int userId = GetCurrentUserId();
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

            // Grouping the items by vendor alias.
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

            // Calling the Bank API.
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44321/");
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                string jsonContent = JsonConvert.SerializeObject(transactionRequest);
                var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/InitiateTransaction", contentString);

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

                var responseContent = await response.Content.ReadAsAsync<TransactionResponse>();
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
                Log.Warn($"Error with completing the order. {ex}");
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        // POST: /Manage/RefundOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RefundOrder(int orderID)
        {
            int userId = GetCurrentUserId();
            Order order = await _dbContext.GetOrderAsync(orderID);
            var orderItems = order.OrderItems;
            string customerAlias = order.StoreAccount.Alias;

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error with processing the refund.";
                return RedirectToAction("Index");
            }

            // Serializing the request for the payload.
            RefundRequest refundRequest = new RefundRequest
            {
                Certificates = orderItems.Select(oi => oi.Certificate).ToList(),
                Amounts = orderItems.Select(oi => oi.TotalPrice).ToList()
            };

            string encryptedAlias = _cryptography.EncryptValue(customerAlias);

            // Calling the Bank API.
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44321/");
                client.DefaultRequestHeaders.Add("Authorization", $"Alias {encryptedAlias}");
                string jsonContent = JsonConvert.SerializeObject(refundRequest);
                var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/InitiateRefund", contentString);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warn($"Refund unsuccessful for order #{orderID}.");
                    TempData["Message"] = $"Failed to refund this order.";
                    return RedirectToAction("Index");
                }

                Log.Warn($"Refund success for order #{orderID}.");
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
                    Log.Warn($"Error with refunding the order. {ex}");
                    return View("CustomError");
                }
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(GetCurrentUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(GetCurrentUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(GetCurrentUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(GetCurrentUserId(), true);
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(GetCurrentUserId(), false);
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(GetCurrentUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(GetCurrentUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(GetCurrentUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(GetCurrentUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        // GET: /Manage/ChangeEmail
        public ActionResult ChangeEmail()
        {
            return View();
        }

        // POST: /Manage/ChangeEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());

            if (user.Email.ToLowerInvariant() != model.OldEmail.ToLowerInvariant())   // ASP.NET Identity uses case-sensitivity with its fields for its DBContext and management, lower case the conditions.
            {
                ModelState.AddModelError(string.Empty, "Old email doesn't match the current one.");
                return View(model);
            }

            user.Email = model.NewEmail;
            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangeEmailSuccess });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            return View(model);
        }

        // GET: /Manage/ChangeUsername
        public ActionResult ChangeUsername()
        {
            return View();
        }

        // POST: /Manage/ChangeUsername
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());

            if (user.UserName != model.OldUsername)
            {
                ModelState.AddModelError(string.Empty, "Old username doesn't match the current one.");
                return View(model);
            }

            user.UserName = model.NewUsername;
            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangeUsernameSuccess });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            return View(model);
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(GetCurrentUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(GetCurrentUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(GetCurrentUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(GetCurrentUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(GetCurrentUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(GetCurrentUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(GetCurrentUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(GetCurrentUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(GetCurrentUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            ChangeEmailSuccess,
            ChangeUsernameSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}