using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace CSEInverter
{
    [CategoryOrder("Status", 1)]
    public class Chunkable
    {
        [Category("Status"), DisplayName("Opdater Status Bar"), PropertyOrder(0), Description("Slå Opdateringer Fra For At Få Mere Fart")]
        public bool UpdateProgress { get; set; } = true;

        [Category("Status"), DisplayName("Skridt På Status Bar"), PropertyOrder(1), Description("Hvor Mange Gange Skal Status Baren Opdateres (Færre opdateringer, mere fart)")]
        public int StepsOnProgressBar { get; set; } = 40;

        [Category("Hukommelse"), DisplayName("Buffer Størrelse"), PropertyOrder(0), Description("Hvor Stor Skal Overførings Bufferen være (Generelt forbruger den mere hukommelse, hvis den er større, men arbejder hurtigere)")]
        public int BufferSize { get; set; } = 8_192;
    }
}