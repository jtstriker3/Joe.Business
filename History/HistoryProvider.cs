using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Joe.Map;

namespace Joe.Business.History
{
    public abstract class HistoryProvider
    {
        protected IEmailProvider EmailProvider { get; set; }
        public static HistoryProvider Instance { get; private set; }

        public static void RegisterHistoryProvider<TContext>()
             where TContext : IDBViewContext, new()
        {
            Instance = new HistoryProvider<TContext>();
        }

        public abstract void ProcessHistory<TModel>(TModel model);
    }

    public class HistoryProvider<TContext> : HistoryProvider
         where TContext : IDBViewContext, new()
    {
        internal protected HistoryProvider()
        {

        }

        public override void ProcessHistory<TModel>(TModel model)
        {
            {
                var historyAttribute = typeof(TModel).GetCustomAttribute<HistoryAttribute>();

                if (historyAttribute != null)
                {
                    if (model is IHistoryId)
                    {
                        using (var context = new TContext())
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
    }
}
