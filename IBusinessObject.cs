using System;
using System.Collections.Generic;
using System.Linq;
using Joe.Map;
using System.Linq.Expressions;
using Joe.MapBack;

namespace Joe.Business
{
    public interface IBusinessObject
    {
        void SetCrud(Object viewModel, Boolean listMode = false);
        void MapBOFunction(Object viewModel, Boolean getModel = true);
    }


    public interface IBusinessObject<TModel, TViewModel, TRepository> : IBusinessObject, IDisposable
        where TModel : class, new()
        where TViewModel : class, new()
        where TRepository : class, IDBViewContext, new()
    {
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelListEvent ViewModelListRetrieved;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelEvent ViewModelCreated;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelEvent ViewModelUpdated;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelEvent ViewModelRetrieved;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelEvent ViewModelDeleted;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelEvent ViewModelMapped;
        event BusinessObject<TModel, TViewModel, TRepository>.ViewModelListEvent ViewModelListMapped;
        TViewModel Create(TViewModel viewModel);
        void Delete(params Object[] ids);
        void Delete(TViewModel viewModel);
        Boolean Exists(TViewModel viewModel);
        IQueryable<TViewModel> Get();
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter);
        IQueryable<TViewModel> Get(int? take, int? skip);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, Boolean setCrudOverride);
        IQueryable<TViewModel> Get(int? take, int? skip, Boolean setCrudOverride);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean setCrudOverride);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy);
        IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy);
        IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy);
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapBOFunctionsOverride = true, Boolean descending = false, params String[] orderBy);
        IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapBOFunctionsOverride = true, Boolean descending = false, String stringFilter = null, params String[] orderBy);
        TViewModel Get(params Object[] ids);
        TViewModel Get(Boolean setCrud = true, params Object[] ids);
        TViewModel Update(TViewModel viewModel);
        IQueryable<TViewModel> Update(List<TViewModel> viewModelList);
        TViewModel Default();
        void MapBOFunction(TViewModel viewModel, Boolean getModel = true);
        void SetCrud(IEnumerable<TViewModel> viewModelList, Boolean iCrud, Boolean listMode = false);
        void SetCrud(TViewModel viewModel, Boolean iCrud, Boolean listMode = false);
    }
}
