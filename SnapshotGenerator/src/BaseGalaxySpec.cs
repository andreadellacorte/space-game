using System;
using System.Collections.Generic;

namespace Demo
{
    public abstract class BaseGalaxySpec
    {
        protected internal abstract IEnumerable<Star> Generate(Random random);
    }
}