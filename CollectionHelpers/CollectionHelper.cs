using MapIndex.Data;
using MapIndex.Infrastructure.ExportHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapIndex.CollectionHelpers
{
    public static class CollectionHelper<T>
    {
        public static List<T> GetProperList(List<IMapIndexData> data)
        {
            List<T> vals = new List<T>(data.Count);

            foreach (IMapIndexData d in data)
            {
                vals.Add((T)d);
            }
            return vals;
        }
        public static List<T> GetNonProperList(List<DataRepo.IMapDataSet> data)
        {
            List<T> vals = new List<T>(data.Count);

            foreach (DataRepo.IMapDataSet d in data)
            {
                vals.Add((T)d);
            }
            return vals;
        }
        public static List<T> GetProperListExport(List<IExportData> data)
        {
            List<T> vals = new List<T>(data.Count);

            foreach (IExportData d in data)
            {
                vals.Add((T)d);
            }
            return vals;
        }
    }
}