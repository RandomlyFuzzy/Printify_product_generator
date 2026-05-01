using System;
using System.Collections.Generic;
using System.Linq;
using PrintifyGenerator.Researcher.Interfaces;

namespace PrintifyGenerator.Researcher.StatCalculators
{
    public class StatCalculatorRegistry
    {
        private readonly List<IStatCalculator> _calculators = new();
        private readonly Dictionary<string, List<IStatCalculator>> _categoryCalculators = new();

        public void Register(IStatCalculator calculator)
        {
            _calculators.Add(calculator);
        }

        public IEnumerable<IStatCalculator> GetAll() => _calculators;

        public IEnumerable<IStatCalculator> GetForDatasetTypes(ISet<string> datasetTypes)
        {
            return _calculators.Where(c => c.RequiredDatasetTypes.All(datasetTypes.Contains));
        }

        public void InitializeForCategory(string category)
        {
            foreach (var calc in _calculators)
                calc.InitializeForCategory(category);
        }

        public void ProcessWordForCategory(string word, Interfaces.ProductContext context)
        {
            foreach (var calc in _calculators)
                calc.ProcessWord(word, context);
        }

        public void ProcessPhraseForCategory(string phrase, Interfaces.ProductContext context)
        {
            foreach (var calc in _calculators)
                calc.ProcessPhrase(phrase, context);
        }

        public void ProcessBrandForCategory(string brand, Interfaces.ProductContext context)
        {
            foreach (var calc in _calculators)
                if (calc is BrandStatCalculator brandCalculator)
                    brandCalculator.ProcessBrand(brand, context);
        }

        public void ProcessColorForCategory(string color, Interfaces.ProductContext context)
        {
            foreach (var calc in _calculators)
                if (calc is ColorStatCalculator colorCalculator)
                    colorCalculator.ProcessColor(color, context);
        }

        public void ProcessMaterialForCategory(string material, Interfaces.ProductContext context)
        {
            foreach (var calc in _calculators)
                if (calc is MaterialStatCalculator materialCalculator)
                    materialCalculator.ProcessMaterial(material, context);
        }

        public void WriteResultsForCategory(string category, System.IO.StreamWriter writer, Dictionary<string, object> additionalData)
        {
            foreach (var calc in _calculators)
                calc.WriteResults(writer, category, additionalData);
        }
    }
}
