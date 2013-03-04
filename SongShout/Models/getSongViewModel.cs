using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace SongShout
{
    public class getSongViewModel
    {
        [Required]
        [DisplayName("Your Twitter Name")]
        public string senderTwitterName { get; set; }
        [Required]
        [DisplayName("Recipient Twitter Name")]
        public string recipientTwitterName { get; set; }
        [DisplayName("Song")]
        public string SearchString { get; set; }
        [Required]
        public string Url { get; set; }
        public string SongId { get; set; }
        public string SongName { get; set; }
        public string ArtistId { get; set; }
        public string ArtistName { get; set; }
        public string AlbumId { get; set; }
        public string AlbumName { get; set; }
         [Required]
        public string Message { get; set; }
        public string FullMessage { get; set; }
        public string RequestToken { get; set; }
        public bool Submitted { get; set; }


    }
}