using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Models
{
    /// <summary>
    /// This was needed since the generated api returns an object when calling the interrogate function on the SD WebUI.
    /// </summary>
    public class InterrogateResult
    {
        public string caption { get; set; } = "";
    }
}
