using System;
using System.Collections.Generic;
using System.Linq;
using Joe.Map;
using System.Linq.Expressions;
using Joe.MapBack;
using System.Collections;

namespace Joe.Business
{
    public interface IRepository
    {
        void SetCrud(Object viewModel, Boolean listMode = false);
        void MapRepoFunction(Object viewModel, Boolean getModel = true);
        IEnumerable Get(String filter = null);
        IDBViewContext CreateContext();
    }


    public interface IRepository<TModel, TViewModel, TContext> : IRepository, IDisposable
        where TModel : class
        where TViewModel : class, new()
        where TContext : IDBViewContext, new()
    {
        event Repository<TModel, TViewModel, TContext>.ViewModelListEvent ViewModelListRetrieved;
        event Repository<TModel, TViewModel, TContext>.ViewModelEvent ViewModelCreated;
        event Repository<TModel, TViewModel, TContext>.ViewModelEvent ViewModelUpdated;
        event Repository<TModel, TViewModel, TContext>.ViewModelEvent ViewModelRetrieved;
        event Repository<TModel, TViewModel, TContext>.ViewModelEvent ViewModelDeleted;
        event Repository<TModel, TViewModel, TContext>.ViewModelEvent ViewModelMapped;
        event Repository<TModel, TViewModel, TContext>.ViewModelListEvent ViewModelListMapped;
        TViewModel Create(TViewModel viewModel, Object dynamicFilters = null);
        void Delete(params Object[] ids);
        void Delete(TViewModel viewModel);
        Boolean Exists(TViewModel viewModel);
        Boolean Exists(params Object[] ids);
        IQueryable<TViewModel> Get();
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, Object dynamicFilter = null, params String[] orderBy);
        IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, String stringFilter = null, Object dynamicFilter = null, Boolean setCount = true, params String[] orderBy);
        TViewModel GetWithFilters(Object dynamicFitler, params Object[] ids);
        TViewModel Get(params Object[] ids);
        TViewModel Get(Object dynamicFilter, Boolean setCrud, params Object[] ids);
        TViewModel Update(TViewModel viewModel, Object dynamicFilters = null);
        IQueryable<TViewModel> Update(List<TViewModel> viewModelList, Object dynamicFilters = null);
        TViewModel Default(TViewModel defaultValues = null);
        void MapRepoFunction(TViewModel viewModel, Boolean getModel = true);
        void SetCrud(IEnumerable<TViewModel> viewModelList, Boolean iCrud, Boolean listMode = false);
        void SetCrud(TViewModel viewModel, Boolean iCrud, Boolean listMode = false);
    }

}
