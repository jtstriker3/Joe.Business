using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Joe.Map;

namespace Joe.Business.History
{
    public class HistoryProvider
    {
        private static HistoryProvider _providerInstance;
        public static HistoryProvider Instance
        {
            get
            {
                _providerInstance = _providerInstance ?? new HistoryProvider();
                return _providerInstance;
            }
        }

        internal protected HistoryProvider()
        {

        }
        public void ProcessHistory<TModel>(TModel model)
        {
            {
                var historyAttribute = typeof(TModel).GetCustomAttribute<HistoryAttribute>();

                if (historyAttribute != null)
                {
                    if (model is IHistoryId)
                    {
                        using (var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<History>())
                        {
                            var historyDBSet = context.GetIPersistenceSet<History>();
                            var iHistoryId = (IHistoryId)model;

                            var newHistory = historyDBSet.Create();

                            var id = iHistoryId.GetID();
                            var previousHistory = historyDBSet.Where(history =>
                                                                           history.Type == typeof(TModel).FullName
                                                                           && history.ID == id).OrderByDescending(history => history.Version).FirstOrDefault();

                            if (previousHistory != null)
                                newHistory.Version = previousHistory.Version + 1;
                            else
                                newHistory.Version = 1;

                            newHistory.ID = iHistoryId.GetID();
                            newHistory.Type = typeof(TModel).FullName;
                            newHistory.UpdateByID = Joe.Security.Security.Provider.UserID;
                            newHistory.Data = Newtonsoft.Json.JsonConvert.SerializeObject(Joe.Map.MapExtensions.Map(model, historyAttribute.ViewType));
                            newHistory.DateSaved = DateTime.Now;

                            historyDBSet.Add(newHistory);
                            context.SaveChanges();
                        }
                    }
                    else
                        throw new InvalidCastException("The type of Entity you are trying to save a history record for does not implement IHistoryId!");
                }
            }
        }

        public History GetLatestHistory<TModel>(TModel model, IDBViewContext context = null)
        {
            var contextNull = context == null;
            var historyAttribute = typeof(TModel).GetCustomAttribute<HistoryAttribute>();
            if (historyAttribute != null)
            {
                if (model is IHistoryId)
                {
                    context = context ?? Configuration.FactoriesAndProviders.ContextFactory.CreateContext<History>();
                    var iHistoryId = (IHistoryId)model;
                    var historyID = iHistoryId.GetID();
                    var history = context.GetIPersistenceSet<History>().OrderByDescending(h => h.Version).FirstOrDefault(h => h.Type == typeof(TModel).FullName && h.ID == historyID);

                    if (contextNull)
                        context.Dispose();

                    return history;

                }
            }

            return null;
        }
    }
}
