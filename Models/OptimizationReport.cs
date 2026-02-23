using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiOptimizerService.Models
{
    public class OptimizationReport
    {
        public int Id { get; set; }
        public string Summary { get; set; }
        public string AISuggestion { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
