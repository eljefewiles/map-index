using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MapIndex.Models.MA;

namespace MapIndex.ChartingData
{
    public interface IChartingData<T>
    {
        IChartOutput GetChartData(List<T> data, MapSearch map);
    }
}