using MapIndex.Infrastructure.Caching;
using MapIndex.Models.MA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MapIndex.Models.ViewModels;

namespace MapIndex.ChartingData.MA
{
    public class MaChartingCacheData : IChartingData<MaData>
    {
        private ICacheProvider _cacheProvider;
        private readonly IChartingData<MaData> _commonRepository;

        public MaChartingCacheData(IChartingData<MaData> commonRepository, ICacheProvider cacheProvider)
        {
            _commonRepository = commonRepository;
            _cacheProvider = cacheProvider;
        }

        public IChartOutput GetChartData(List<MaData> data, MapSearch map)
        {
            try
            {
                return _commonRepository.GetChartData(data, map);

            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}