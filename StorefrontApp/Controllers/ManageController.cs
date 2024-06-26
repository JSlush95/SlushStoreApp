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

            var userId = GetCurrentUserId();
            var user = await UserManager.FindByIdAsync(userId);
            var userStoreAccountCount = _dbContext.StoreAccounts
                .Where(sa => sa.HolderID == userId).Count();
            var userShoppingCartItems = await _dbContext.ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userId)
                .ToListAsync();
            var userOrderItems = await _dbContext.Orders
                .Where(o => o.StoreAccount.HolderID == userId)
                .ToListAsync();
            var accountAlias = await _dbContext.StoreAccounts
                .Where(u => u.HolderID == userId)
                .Select(u => u.Alias)
                .FirstOrDefaultAsync();
            var userPaymentMethods = await _dbContext.PaymentMethods
                .Where(pm => pm.Account.HolderID == userId)
                .ToListAsync();

            bool storeAccountCreated = (userStoreAccountCount == 0) ? false : true;

            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(User.Identity.GetUserId()),
                EmailConfirmed = user.EmailConfirmed,
                StoreAccountCreated = storeAccountCreated,
                Orders = userOrderItems,
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
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error, please fill out the fields with valid data.";
                return RedirectToAction("Index");
            }

            var userStoreAccount = new StoreAccount
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

            var userId = GetCurrentUserId();
            var userAccount = await _dbContext.StoreAccounts
                .Where(sa => sa.HolderID == userId)
                .FirstOrDefaultAsync();
            var existingUser = await _dbContext.StoreAccounts
                .Where(sa => sa.Alias == ChangeAliasInput)
                .FirstOrDefaultAsync();

            if (existingUser != null)
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
            }

            return RedirectToAction("Index");
        }

        // POST: /Manage/AddPaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPaymentMethod(string cardNumber, int? keyID)
        {
            var userId = GetCurrentUserId();

            // Manual validation for cardNumber.
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 11)
            {
                TempData["Message"] = "The Card Number field must be a length of 11.";
                return RedirectToAction("Index");
            }

            // Manual validation for KeyID.
            if (keyID == null || (keyID < 10000 || keyID > 99999))
            {
                TempData["Message"] = "The Key ID field is required and must be between 10000 and 99999 (5 digits).";
                return RedirectToAction("Index");
            }

            var userPaymentMethod = new PaymentMethod
            {
                AccountID = userId,
                CardNumber = cardNumber,
                KeyID = (int)keyID
            };

            // Duplication check.
            var existingPaymentMethodForSameUser = await _dbContext.PaymentMethods
                .Where(pm => (pm.CardNumber == cardNumber && pm.KeyID == keyID) && pm.Account.HolderID == userId)
                .FirstOrDefaultAsync();

            if (existingPaymentMethodForSameUser != null)
            {
                TempData["Message"] = "Please use a payment method that isn't registered for you already.";
                return RedirectToAction("Index");
            }

            // Encrypting the KeyID.
            var encryptedKeyID = _cryptography.EncryptValue(keyID.ToString());
            var encryptedCardNumber = _cryptography.EncryptValue(cardNumber);

            // Calling the Bank API.
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44321/"); // Bank app's URL
                var response = await client.GetAsync($"api/VerifyCard?encryptedCardNumber={Uri.EscapeDataString(encryptedCardNumber)}&encryptedKeyID={Uri.EscapeDataString(encryptedKeyID)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Card validation failed.";
                    return RedirectToAction("Index");
                }

                var result = await response.Content.ReadAsStringAsync();
                if (result.Contains("Card is not active") || result.Contains("Unauthorized"))
                {
                    TempData["Message"] = result;
                    return RedirectToAction("Index");
                }
            }

            try
            {
                _dbContext.PaymentMethods.Add(userPaymentMethod);
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with creating the payment method.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error with creating the payment method.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePaymentMethod(int paymentMethodID)
        {
            var userPaymentMethod = await _dbContext.PaymentMethods
                .Where(pm => pm.PaymentMethodID == paymentMethodID)
                .FirstOrDefaultAsync();

            var result = _dbContext.PaymentMethods.Remove(userPaymentMethod);

            try
            {
                await _dbContext.SaveChangesAsync();
                TempData["Message"] = "Success with removing the payment method.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error with removing the payment method.";
                return View("CustomError");
            }

            return RedirectToAction("Index");
        }

        // GET: /Manage/CreateOrder
        public async Task<ActionResult> CreateOrder()
        {
            var userId = GetCurrentUserId();

            var shoppingCartItems = await _dbContext.ShoppingCartsItems
                .Where(sci => sci.ShoppingCart.Account.HolderID == userId)
                .ToListAsync();

            var paymentMethods = await _dbContext.PaymentMethods
                .Where(pm => pm.Account.HolderID == userId)
                .ToListAsync();

            var model = new CreateOrderViewModel
            {
                ShoppingCartItems = shoppingCartItems,
                PaymentMethods = paymentMethods
            };

            return View(model);
        }

        // POST: /Manage/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateOrder(CreateOrderViewModel model)
        {
            var userId = GetCurrentUserId();
            var userCart = await _dbContext.ShoppingCarts
                .Where(sc => sc.Account.HolderID == userId)
                .FirstOrDefaultAsync();
            var paymentMethods = await _dbContext.PaymentMethods
                .Where(pm => pm.Account.HolderID == userId)
                .ToListAsync();
            model.ShoppingCartItems = userCart.ShoppingCartItems.ToList();
            model.PaymentMethods = paymentMethods;

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error with processing the order.";
                return View(model);
            }

            var paymentMethod = _dbContext.PaymentMethods
                .Where(pm => pm.PaymentMethodID == model.SelectedPaymentMethodID)
                .FirstOrDefault();

            var encryptedKeyID = _cryptography.EncryptValue(paymentMethod.KeyID.ToString());
            var encryptedCardNumber = _cryptography.EncryptValue(paymentMethod.CardNumber);

            decimal totalPrice = 0;
            List<OrderItem> items = new List<OrderItem>();

            Order order = new Order
            {
                BuyerID = userId,
                PaymentMethodID = model.SelectedPaymentMethodID,
                CertificatePairs = new List<Certificate>(),
                ShippingAddress = model.ShippingAddress,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in userCart.ShoppingCartItems)
            {
                totalPrice += item.Product.Price * item.Quantity;

                OrderItem orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.Product.ProductID,
                    Quantity = item.Quantity
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
                });

            // Calling the Bank API.
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44321/");
                foreach (var vendor in vendorGroups)
                {
                    var response = await client.GetAsync($"api/InitiateTransaction?encryptedCardNumber={Uri.EscapeDataString(encryptedCardNumber)}&encryptedKeyID={Uri.EscapeDataString(encryptedKeyID)}&vendorAccountAlias={Uri.EscapeDataString(vendor.VendorAlias)}&paymentAmount={vendor.TotalAmount}");

                    string content = await response.Content.ReadAsStringAsync();

                    if (content.Contains("Not enough funds to complete the purchase."))
                    {
                        ViewBag.Message = $"Not enough money to complete the transaction.";
                        return View(model);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Warn($"Card validation failed for vendor {vendor.VendorAlias}.");
                        ViewBag.Message = $"Card validation failed.";
                        return View(model);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        Log.Warn($"The server failed to validate the payment information for vendor {vendor.VendorAlias}.");
                        ViewBag.Message = $"The server failed to validate the payment information.";
                        return View(model);
                    }

                    string certificate = content.Substring(content.IndexOf("Certificate:") + 12).Trim();
                    var customerAlias = await _dbContext.StoreAccounts
                        .Where(sa => sa.HolderID == userId)
                        .Select(sa => sa.Alias)
                        .FirstOrDefaultAsync();

                    Certificate certificateItem = new Certificate
                    {
                        CertificateValue = certificate,
                        VendorAlias = vendor.VendorAlias,
                        CustomerAlias = customerAlias
                    };

                    order.CertificatePairs.Add(certificateItem);
                }
            }
            order.OrderItems = items;
            order.TotalPrice = totalPrice;
            order.PurchaseDate = DateTime.Now;
            order.Status = OrderStatus.Approved;

            try
            {
                _dbContext.Orders.Add(order);
                _dbContext.ShoppingCartsItems.RemoveRange(userCart.ShoppingCartItems);
                await _dbContext.SaveChangesAsync();
            }catch (Exception ex)
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
            var refunderAlias = await _dbContext.Orders
                .Where(o => o.OrderID == orderID)
                .Select(o => o.StoreAccount.Alias)
                .FirstOrDefaultAsync();

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Error with processing the refund.";
                return RedirectToAction("Index");
            }

            // ...

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