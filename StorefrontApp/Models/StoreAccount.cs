using BankingApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontApp.Models
{
    [Table("StoreAccounts")]
    public class StoreAccount
    {
        [Key]
        public int AccountID { get; set; }
        [Required]
        public int HolderID {  get; set; }
        [Required]
        public DateTime DateOpened { get; set; }
        [Required]
        public AccountType AccountType { get; set; }
        
        [ForeignKey("HolderID")]
        public virtual User User { get; set; }
    }

    public enum AccountType
    {
        Personal,
        Business
    }
}