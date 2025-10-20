using System.ComponentModel.DataAnnotations;

namespace App.Helper.Dto
{

    public class PrintDto
    {
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal Total => Price * Qty;
    }
}

