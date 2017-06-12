using System.Collections.Generic;
using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.Kernel.Special;

// Summary:     This class contains a set of static methods used to automatically generate input menus
// ===============================================================================
// Methods:     TopoSelect (written by Aidan)       - Menu for unit cell topologhy
//              GradientSelect (written by Aidan)   - Menu for thickness gradient expressions
// ===============================================================================
// Author(s):   Brendan Epley, Jacques Perrault
//              Based on IntraLattice by Aidan Kurtz

namespace IntraLattice.CORE.Helpers
{
    public class InputTools
    {
        /// <summary>
        /// Generates selection list for preset unit cell topologies.
        /// </summary>
        /// <param name="index">Component input index. (first input is index 0)</param>
        /// <param name="offset">Vertical offset of the menu, to help with positioning.</param>
        public static void TopoSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index, float offset)
        {
            // Instantiate  new value list
            var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.Cycle;
            vallist.CreateAttributes();

            // Customise value list position
            float xCoord = Component.Attributes.Pivot.X - 250;
            float yCoord = Component.Attributes.Pivot.Y + index * 40 - offset;
            PointF cornerPt = new PointF(xCoord, yCoord);
            vallist.Attributes.Pivot = cornerPt;

            // Populate value list with our own data
            vallist.ListItems.Clear();
            var items = new List<GH_ValueListItem>();
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Grid", "0"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("X", "1"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Star", "2"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cross", "3"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Tesseract", "4"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Vintiles", "5"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Octet", "6"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Diamond", "7"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Honeycomb 1", "8"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Honeycomb 2", "9"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("A - Team", "10"));

            vallist.ListItems.AddRange(items);

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(vallist, false);

            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(vallist);
            Component.Params.Input[index].CollectData();
        }
    }

}