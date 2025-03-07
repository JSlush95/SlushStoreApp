﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace StorefrontAppCore.Models
{
    [Table("StoreAccounts")]
    public class StoreAccount
    {
        [Key]
        public int AccountID { get; set; }
        [Required]
        public int? HolderID {  get; set; }
        [MaxLength(256)]
        public string Alias { get; set; }
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
        Business,
        Dummy
    }
}