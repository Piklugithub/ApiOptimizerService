using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiOptimizerService.Models
{
    public class ApiMetric
    {
        public int Id { get; set; }
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
