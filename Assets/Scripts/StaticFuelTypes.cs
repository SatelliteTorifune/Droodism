using System.Xml.Linq;

namespace Assets.Scripts
{
    public static class StaticFuelTypes
    {
        public static XElement WaterFuel = new XElement("Fuel",
            new XAttribute("id", "H2O"),
            new XAttribute("name", "Water"),
            new XAttribute("gamma", "1.21"),
            new XAttribute("density", "1.0"),
            new XAttribute("molecularWeight", "11.8"),
            new XAttribute("combustionTemperature", "3304"),
            new XAttribute("price", "0.01"),
            new XAttribute("explosivePower", "0"),
            new XAttribute("description", "Now that's what I call high-quality H2O."),
            new XElement("Visual",
                new XAttribute("exhaustColor", "#ffffff60"),
                new XAttribute("exhaustColorExpanded", "#ffffffff"), 
                new XAttribute("exhaustColorTip", "#aaffff40"),      
                new XAttribute("exhaustColorShock", "#aaffff60"),    
                new XAttribute("exhaustColorFlame", "#ffffffff"),    
                new XAttribute("exhaustColorSoot", "#ffffffff"),     
                new XAttribute("exhaustColorSmoke", "#ffffff00"),    
                new XAttribute("globalIntensity", "1"),              
                new XAttribute("shockIntensity", "1"),               
                new XAttribute("rimShade", "0.95")                   
            )
        );

        public static XElement oxygenFuelType = new XElement("FuelType",
            new XAttribute("id", "Oxygen"),
            new XAttribute("name", "Oxygen"),
            new XAttribute("gamma", "0"),
            new XAttribute("density", "0.001429"),
            new XAttribute("molecularWeight", "32"),
            new XAttribute("combustionTemperature", "0"),
            new XAttribute("price", "20"),
            new XAttribute("explosivePower", "0"),
            new XAttribute("description", "Drood breathe this."),
            new XElement("Visual",
                new XAttribute("exhaustColor", "#00000000"),
                new XAttribute("exhaustColorTip", "#00000000"),
                new XAttribute("exhaustColorShock", "#00000000"),
                new XAttribute("exhaustColorFlame", "#00000000"),
                new XAttribute("exhaustColorSoot", "#00000000"),
                new XAttribute("exhaustColorSmoke", "#ffffff00"),
                new XAttribute("globalIntensity", "0"),
                new XAttribute("shockIntensity", "-20"),
                new XAttribute("rimShade", "0")
            )
        );
        public static XElement foodFuel = new XElement("Fuel",
    new XAttribute("id", "Food"),
    new XAttribute("name", "Food"),
    new XAttribute("gamma", "0"),
    new XAttribute("density", "1.5"),
    new XAttribute("molecularWeight", "128"),
    new XAttribute("combustionTemperature", "0"),
    new XAttribute("price", "25"),
    new XAttribute("explosivePower", "0"),
    new XAttribute("description", "The heavy compressed food Drood eat "),
    new XElement("Visual",
        new XAttribute("exhaustColor", "#00000000"),
        new XAttribute("exhaustColorTip", "#00000000"),
        new XAttribute("exhaustColorShock", "#00000000"),
        new XAttribute("exhaustColorFlame", "#00000000"),
        new XAttribute("exhaustColorSoot", "#00000000"),
        new XAttribute("exhaustColorSmoke", "#ffffff00"),
        new XAttribute("globalIntensity", "0"),
        new XAttribute("shockIntensity", "-20"),
        new XAttribute("rimShade", "0")
    )
);

public static XElement wastedWaterFuel = new XElement("Fuel",
    new XAttribute("id", "Wasted Water"),
    new XAttribute("name", "Wasted Water"),
    new XAttribute("gamma", "0"),
    new XAttribute("density", "1.05"),
    new XAttribute("molecularWeight", "34"),
    new XAttribute("combustionTemperature", "0"),
    new XAttribute("price", "0"),
    new XAttribute("explosivePower", "0"),
    new XAttribute("description", "Wasted Water Drood Generated"),
    new XElement("Visual",
        new XAttribute("exhaustColor", "#00000000"),
        new XAttribute("exhaustColorTip", "#00000000"),
        new XAttribute("exhaustColorShock", "#00000000"),
        new XAttribute("exhaustColorFlame", "#00000000"),
        new XAttribute("exhaustColorSoot", "#00000000"),
        new XAttribute("exhaustColorSmoke", "#ffffff00"),
        new XAttribute("globalIntensity", "0"),
        new XAttribute("shockIntensity", "-20"),
        new XAttribute("rimShade", "0")
    )
);

public static XElement co2Fuel = new XElement("Fuel",
    new XAttribute("id", "CO2"),
    new XAttribute("name", "Carbon Dioxide"),
    new XAttribute("gamma", "0"),
    new XAttribute("density", "0.001977"),
    new XAttribute("molecularWeight", "44"),
    new XAttribute("combustionTemperature", "0"),
    new XAttribute("price", "10"),
    new XAttribute("explosivePower", "0"),
    new XAttribute("description", "Carbon Dioxide Drood Generated"),
    new XElement("Visual",
        new XAttribute("exhaustColor", "#00000000"),
        new XAttribute("exhaustColorTip", "#00000000"),
        new XAttribute("exhaustColorShock", "#00000000"),
        new XAttribute("exhaustColorFlame", "#00000000"),
        new XAttribute("exhaustColorSoot", "#00000000"),
        new XAttribute("exhaustColorSmoke", "#ffffff00"),
        new XAttribute("globalIntensity", "0"),
        new XAttribute("shockIntensity", "-20"),
        new XAttribute("rimShade", "0")
    )
);

public static XElement solidWasteFuel = new XElement("Fuel",
    new XAttribute("id", "Solid Waste"),
    new XAttribute("name", "Solid Waste"),
    new XAttribute("gamma", "0"),
    new XAttribute("density", "2"),
    new XAttribute("molecularWeight", "100"),
    new XAttribute("combustionTemperature", "0"),
    new XAttribute("price", "0"),
    new XAttribute("explosivePower", "15"),
    new XAttribute("description", "Solid Waste Drood Generated,or you can call it sh1t"),
    new XElement("Visual",
        new XAttribute("exhaustColor", "#00000000"),
        new XAttribute("exhaustColorTip", "#00000000"),
        new XAttribute("exhaustColorShock", "#00000000"),
        new XAttribute("exhaustColorFlame", "#00000000"),
        new XAttribute("exhaustColorSoot", "#00000000"),
        new XAttribute("exhaustColorSmoke", "#ffffff00"),
        new XAttribute("globalIntensity", "0"),
        new XAttribute("shockIntensity", "-20"),
        new XAttribute("rimShade", "0")
    )
);

    }
}