using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrintifyGenerator.Researcher.Interfaces
{
    public interface IDatasetProvider
    {
        string Name { get; }
        string Type { get; }
        Task LoadAsync();
        bool HasProductData(string asin);
        T? GetProductData<T>(string asin) where T : class;
        IEnumerable<string> GetAllAsins();
    }

    public class DatasetRegistry
    {
        private readonly Dictionary<string, IDatasetProvider> _datasets = new();

        public void Register(IDatasetProvider dataset)
        {
            _datasets[dataset.Type] = dataset;
        }

        public IDatasetProvider? Get(string type) => _datasets.TryGetValue(type, out var d) ? d : null;
        public IEnumerable<IDatasetProvider> GetAll() => _datasets.Values;
        public IEnumerable<IDatasetProvider> GetByTypes(ISet<string> types) =>
            _datasets.Values.Where(d => types.Contains(d.Type));
    }
}
