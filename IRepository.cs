using System;
using System.Collections.Generic;
using System.Linq;
using Joe.Map;
using System.Linq.Expressions;
using Joe.MapBack;
using System.Collections;

namespace Joe.Business
{
    public interface IRepository : IDisposable
    {
        void SetCrud(Object viewModel, Boolean listMode = false);
        void MapRepoFunction(Object viewModel, Boolean getModel = true);
        IEnumerable Get(String filter = null);
        IDBViewContext CreateContext();
    }

    public interface IRepository<TModel, TViewModel> : IRepository
        where TModel : class
        where TViewModel : class, new()
    {
        event Repository<TModel, TViewModel>.ViewModelListEvent ViewModelListRetrieved;
        event Repository<TModel, TViewModel>.ViewModelEvent ViewModelCreated;
        event Repository<TModel, TViewModel>.ViewModelEvent ViewModelUpdated;
        event Repository<TModel, TViewModel>.ViewModelEvent ViewModelRetrieved;
        event Repository<TModel, TViewModel>.ViewModelEvent ViewModelDeleted;
        event Repository<TModel, TViewModel>.ViewModelEvent ViewModelMapped;
        event Repository<TModel, TViewModel>.ViewModelListEvent ViewModelListMapped;
        Result<TViewModel> Create(TViewModel viewModel, Object dynamicFilters = null);
        void Delete(params Object[] ids);
        void Delete(TViewModel viewModel);
        Boolean Exists(TViewModel viewModel);
        Boolean Exists(params Object[] ids);
        IQueryable<TViewModel> Get();
        IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null, Expression<Func<TModel, Boolean>> sourceFilter = null,  int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, String stringFilter = null, Object dynamicFilter = null, params String[] orderBy);
        IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter = null, Expression<Func<TModel, Boolean>> sourceFilter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, String stringFilter = null, Object dynamicFilter = null, Boolean setCount = true, params String[] orderBy);
        TViewModel GetWithFilters(Object dynamicFitler, params Object[] ids);
        TViewModel Get(params Object[] ids);
        TViewModel Get(Object dynamicFilter, Boolean setCrud, params Object[] ids);
        Result<TViewModel> Update(TViewModel viewModel, Object dynamicFilters = null);
        IEnumerable<Result<TViewModel>> Update(List<TViewModel> viewModelList, Object dynamicFilters = null);
        TViewModel Default(TViewModel defaultValues = null);
        void MapRepoFunction(TViewModel viewModel, Boolean getModel = true);
        void SetCrud(IEnumerable<TViewModel> viewModelList, Boolean iCrud);
        void SetCrud(TViewModel viewModel, Boolean iCrud, bool forList);
    }
}
