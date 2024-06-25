﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using CompareAttribute = System.ComponentModel.DataAnnotations.CompareAttribute;

namespace StorefrontApp.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool StoreAccountCreated { get; set; }
        [Display(Name = "Alias Name")]
        public string ChangeAliasInput {  get; set; }
        public string AliasName { get; set; }
        [Required]
        [StringLength(11)]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }
        [Required]
        [Display(Name = "Key ID")]
        [Range(10000, 99999)]
        public int KeyID { get; set; }
        [Required]
        [Display(Name = "Account Type")]
        public AccountType AccountTypeInput { get; set; }
        [Required]
        [Display(Name = "Account Alias")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string AccountAliasInput { get; set; }
        public List<ShoppingCartItem> ShoppingCartItems { get; set; }
        public List<Order> Orders { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeEmailViewModel
    {
        [Required]
        [Display(Name = "Current email")]
        public string OldEmail { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "The {0} field is not a valid email address.")]
        [Display(Name = "New email")]
        public string NewEmail { get; set; }

        [Display(Name = "Confirm new email")]
        [System.ComponentModel.DataAnnotations.Compare("NewEmail", ErrorMessage = "The new email and confirmation email do not match.")]
        public string ConfirmEmail { get; set; }
    }

    public class ChangeUsernameViewModel
    {
        [Required]
        [Display(Name = "Current username")]
        public string OldUsername { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New username")]
        public string NewUsername { get; set; }

        [Display(Name = "Confirm new username")]
        [System.ComponentModel.DataAnnotations.Compare("NewUsername", ErrorMessage = "The new username and confirmation username do not match.")]
        public string ConfirmUsername { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class CreateOrderViewModel
    {
        [Required]
        [Display(Name = "Current Payment Methods")]
        public int SelectedPaymentMethodID {  get; set; }
        [Required]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; }
        public List<ShoppingCartItem> ShoppingCartItems { get; set; }
    }

    public class CreateStoreAccountViewModel
    {
        [Required]
        [Display(Name = "Account Type")]
        public AccountType AccountTypeInput { get; set; }

        [Required]
        [Display(Name = "Account Alias")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string AccountAliasInput { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }
}