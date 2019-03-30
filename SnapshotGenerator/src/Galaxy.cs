using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Demo
{
    public class Galaxy
    {
        public IEnumerable<Star> Stars { get; private set; }

        private Galaxy(IEnumerable<Star> stars)
        {
            Stars = stars;
        }        

        public static Galaxy Generate(BaseGalaxySpec spec, Random random)
        {
            var s = spec.Generate(random);

            return new Galaxy(s);
        }
    }
}