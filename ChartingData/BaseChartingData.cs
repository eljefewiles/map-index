using MapIndex.Models.MA;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapIndex.ChartingData
{
    public abstract class BaseChartingData<T>
    {
        protected IChartOutput GetChartData(List<T> data, MapSearch map)
        {
            return null;
        }
    }
}