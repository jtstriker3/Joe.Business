using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Joe.Map;
using System.Data.Entity;
using Joe.Security;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using Joe.MapBack;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Validation;

namespace Joe.Business
{

    public abstract class Repository : IRepository
    {
        public abstract void SetCrud(Object viewModel, Boolean listMode = false);
        public abstract void MapRepoFunction(Object viewModel, Boolean getModel = true);
        public abstract IEnumerable Get(String filter = null);
        public static IEmailProvider EmailProvider { get; set; }
        public static ISecurityFactory _securityFactory;
        public static ISecurityFactory RepoSecurityFactory
        {
            get
            {
                _securityFactory = _securityFactory ?? Joe.Security.SecurityFactory.Instance;
                return _securityFactory;
            }
            set
            {
                _securityFactory = value;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RepositoryType">Must Already Contain generic Parameters</param>
        /// <returns></returns>
        public static IRepository CreateRepo(Type RepositoryType)
        {
            var repoInterface = RepositoryType.GetInterface("IRepository`3");
            var model = repoInterface.GetGenericArguments().First();
            var viewModel = repoInterface.GetGenericArguments().ToArray()[1];
            var repository = repoInterface.GetGenericArguments().ToArray()[2];
            var genericRepo = typeof(Repository<,,>).MakeGenericType(model, viewModel, repository);
            var method = genericRepo.GetMethod("CreateRepo");
            var repo = (IRepository)method.Invoke(null, new Object[] { RepositoryType });

            return repo;
        }
        public static IRepository CreateRepo(Type RepositoryType, Type model, Type viewModel, Type repository)
        {
            var genericRepo = typeof(Repository<,,>).MakeGenericType(model, viewModel, repository);
            var method = genericRepo.GetMethod("CreateRepo");
            var repo = (IRepository)method.Invoke(null, new Object[] { RepositoryType });

            return repo;
        }
        protected static Object CreateObject(Type type)
        {

            var newExpression = Expression.New(type);
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return lambdaExpression.Compile().DynamicInvoke();
        }
    }


    public abstract class Repository<TModel, TViewModel, TContext> : Repository, IRepository<TModel, TViewModel, TContext>
        where TModel : class, new()
        where TViewModel : class, new()
        where TContext : class, IDBViewContext, new()
    {
        private IDbSet<TModel> _source;
        protected virtual IDbSet<TModel> Source
        {
            get
            {
                return _source ?? this.Context.GetIDbSet<TModel>();
            }
            set { _source = value; }
        }
        protected BusinessConfigurationAttribute Configuration { get; set; }
        protected virtual ISecurity<TModel> Security { get; set; }
        private TContext _repository;
        protected TContext Context
        {
            get
            {
                _repository = _repository ?? new TContext();
                return _repository;
            }
            set
            {
                _repository = value;
            }
        }

        #region Delegates
        protected delegate void MapDelegate(TModel model, TViewModel viewModel, TContext repository);
        protected delegate void SaveDelegate(TModel model, TViewModel viewModel, TContext repository);
        protected delegate void AfterGetDelegate(TViewModel viewModel, TContext repository);
        protected delegate void BeforeGetDelegate(TContext repository);
        protected delegate IQueryable<TViewModel> GetListDelegate(IQueryable<TViewModel> viewModels, TContext repository);
        protected SaveDelegate BeforeUpdate;
        protected SaveDelegate BeforeDelete;
        protected SaveDelegate BeforeCreate;
        protected SaveDelegate AfterUpdate;
        protected SaveDelegate AfterDelete;
        protected MapDelegate BeforeMapBack;
        protected MapDelegate AfterMap;
        protected SaveDelegate AfterCreate;
        protected BeforeGetDelegate BeforeGet;
        protected AfterGetDelegate AfterGet;
        protected GetListDelegate BeforeReturnList;
        #endregion

        #region Events
        public delegate IQueryable<TViewModel> ViewModelListEvent(Object sender, ViewModelListEventArgs<TViewModel> viewModelListEventArgs);
        public delegate void ViewModelEvent(Object sender, ViewModelEventArgs<TViewModel> viewModelEventArgs);
        public event ViewModelListEvent ViewModelListRetrieved;
        public event ViewModelEvent ViewModelCreated;
        public event ViewModelEvent ViewModelUpdated;
        public event ViewModelEvent ViewModelRetrieved;
        public event ViewModelEvent ViewModelDeleted;
        public event ViewModelEvent ViewModelMapped;
        public event ViewModelListEvent ViewModelListMapped;
        #endregion

        public new static IRepository<TModel, TViewModel, TContext> CreateRepo(Type RepositoryType)
        {
            IRepository<TModel, TViewModel, TContext> repo;
            if (RepositoryType.GetGenericArguments().Count() == 2)
                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TViewModel), typeof(TContext)));
            else if (RepositoryType.GetGenericArguments().Count() == 1)
                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TContext)));
            else
                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TModel), typeof(TViewModel), typeof(TContext)));

            return repo;
        }

        public Repository(ISecurity<TModel> security, TContext repositiory)
        {
            Configuration = (BusinessConfigurationAttribute)GetType().GetCustomAttributes(typeof(BusinessConfigurationAttribute), true).SingleOrDefault() ?? new BusinessConfigurationAttribute();
            Context = repositiory;
            Security = security ?? this.TryGetSecurityForModel();
        }

        public Repository(ISecurity<TModel> security)
            : this(security, null)
        {

        }

        public Repository(TContext repositiory)
            : this(new Security<TModel>(), repositiory)
        {

        }

        public Repository() :
            this(null, null)
        {

        }

        private static IEnumerable<Type> Types { get; set; }
        private static Type SecurityType { get; set; }

        private ISecurity<TModel> TryGetSecurityForModel()
        {
            if (this.Configuration.SecurityType != null)
            {
                var securityType = this.Configuration.SecurityType.IsGenericType ? this.Configuration.SecurityType.MakeGenericType(typeof(TModel)) : this.Configuration.SecurityType;
                return (ISecurity<TModel>)Expression.Lambda(Expression.Block(Expression.New(securityType))).Compile().DynamicInvoke();
            }

            var security = RepoSecurityFactory.Create<TModel>();
            if (security == null)
            {
                if (Configuration.DefualtSecurityType != null)
                {
                    var securityType = this.Configuration.DefualtSecurityType.IsGenericType ? this.Configuration.DefualtSecurityType.MakeGenericType(typeof(TModel)) : this.Configuration.DefualtSecurityType;
                    security = (ISecurity<TModel>)Expression.Lambda(Expression.Block(Expression.New(securityType))).Compile().DynamicInvoke();
                }
                else
                    security = new Security<TModel>();
            }
            return security;
        }

        public virtual TViewModel Create(TViewModel viewModel, Object dynamicFilters = null)
        {
            try
            {
                var model = Source.Create();

                if (Configuration.IncrementKey)
                    SetNewKey(viewModel);


                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(model, viewModel, Context);

                model.MapBack(viewModel, this.Context);
                model = this.Source.Add(model);

                if (this.BeforeCreate != null)
                    this.BeforeCreate(model, viewModel, Context);

                if (!this.Configuration.UseSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                    viewModel = model.Map<TModel, TViewModel>(dynamicFilters);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);


                if (this.AfterCreate != null)
                    this.AfterCreate(model, viewModel, Context);
                if (this.ViewModelCreated != null)
                    this.ViewModelCreated(this, new ViewModelEventArgs<TViewModel>(viewModel));

                FlushViewModelCache();
                return viewModel;
            }
            catch (Exception ex)
            {
                if (ex is ValidationException
                    || ex is DbEntityValidationException
                    || ex is DbUnexpectedValidationException)
                    throw ex;

                throw new Exception("Error Creating: " + typeof(TModel).Name, ex);
            }
        }

        #region GetList

        public virtual IQueryable<TViewModel> Get()
        {
            return Get((Expression<Func<TViewModel, Boolean>>)null);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter)
        {
            return Get(filter, null, null, Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, Boolean setCrudOverride)
        {
            return Get(filter, null, null, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get(int? take, int? skip)
        {
            return Get((Expression<Func<TViewModel, Boolean>>)null, take, skip, this.Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get(int? take, int? skip, Boolean setCrudOverride)
        {
            return Get((Expression<Func<TViewModel, Boolean>>)null, take, skip, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean setCrudOverride)
        {
            int count;
            return Get(out count, filter, take, skip, false, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip)
        {
            return Get(filter, take, skip, this.Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy)
        {
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
        {
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, Object dyanmicFilter = null, params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, setCrudOverride, mapRepoFunctionsOverride, descending, null, dyanmicFilter, orderBy);
        }

        /// <summary>
        /// Get a list of ViewModels
        /// </summary>
        /// <returns>Returns a list of ViewModesl. If UseSecurity and SetCrud are set to true then the list will be filtered to only return results the user has access too.</returns>
        public virtual IQueryable<TViewModel> Get(out int count,
            Expression<Func<TViewModel, Boolean>> filter = null,
            int? take = null, int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapRepoFunctionsOverride = true,
            Boolean descending = false,
            String stringfilter = null,
            Object dyanmicFilters = null,
            params String[] orderBy
            )
        {
            IQueryable<TViewModel> viewModels;
            if (Configuration.GetListFromCache)
                viewModels = StaticCacheHelper.GetCache<TViewModel>().AsQueryable();
            else
                viewModels = this.Source.Map<TModel, TViewModel>(dyanmicFilters);

            if (filter != null)
                viewModels = viewModels.Where(filter);
            if (!String.IsNullOrEmpty(stringfilter))
                viewModels = viewModels.Filter(stringfilter);
            count = viewModels.Count();

            if (this.Configuration.SetCrud && setCrudOverride)
            {
                var viewModelList = viewModels.ToList();

                this.SetCrud(viewModels, this.ImplementsICrud, true);

                if (this.Configuration.UseSecurity)
                {
                    viewModels = viewModelList.Where(vm => this.Security.CanRead(this.GetModel, vm)).AsQueryable();
                    count = viewModels.Count();
                }
            }

            if (orderBy.Count() > 0 && orderBy.First() != null)
                if (!descending)
                    viewModels = viewModels.OrderBy(orderBy);
                else
                    viewModels = viewModels.OrderByDescending(orderBy);
            if (skip.HasValue)
                viewModels = viewModels.Skip(skip.Value);
            if (take.HasValue)
                viewModels = viewModels.Take(take.Value);

            if (ViewModelListMapped != null)
                viewModels = ViewModelListMapped(this, new ViewModelListEventArgs<TViewModel>(viewModels));

            if (this.Configuration.MapRepositoryFunctionsForList && mapRepoFunctionsOverride)
                viewModels.ForEach(vm => this.MapRepoFunction(vm));

            if (this.BeforeReturnList != null)
                viewModels = this.BeforeReturnList(viewModels, this.Context);
            if (this.ViewModelListRetrieved != null)
                viewModels = ViewModelListRetrieved(this, new ViewModelListEventArgs<TViewModel>(viewModels));

            return viewModels;
        }

        public override IEnumerable Get(string filter = null)
        {
            int count = 0;
            return this.Get(out count, stringfilter: filter, setCrudOverride: false, mapRepoFunctionsOverride: false);
        }

        #endregion

        public virtual TViewModel Get(params Object[] ids)
        {
            return Get(null, true, ids);
        }

        public virtual TViewModel GetWithFilters(Object dynamicFilters, params Object[] ids)
        {
            return Get(dynamicFilters, true, ids);
        }

        public virtual TViewModel Get(Object dyanmicFilters, Boolean setCrud, params Object[] ids)
        {
            try
            {
                TViewModel viewModel;

                if (this.BeforeGet != null)
                    this.BeforeGet(this.Context);
                var model = this.Source.Find(this.GetTypedIDs(ids));
                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);
                if (AfterMap != null)
                    AfterMap(model, viewModel, Context);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                this.MapRepoFunction(viewModel);

                if (this.Configuration.SetCrud && setCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterGet != null)
                    this.AfterGet(viewModel, this.Context);
                if (!this.Configuration.UseSecurity || this.Security.CanRead(this.GetModel, viewModel))
                {
                    if (this.ViewModelRetrieved != null)
                        this.ViewModelRetrieved(this, new ViewModelEventArgs<TViewModel>(viewModel));
                    return viewModel;
                }
                else
                    throw new System.Security.SecurityException(String.Format("Access to read denied for: {0}", typeof(TModel).Name));
            }
            catch (Exception ex)
            {
                throw new Exception("Error Getting: " + typeof(TModel).Name, ex);
            }
        }

        public virtual TViewModel Update(TViewModel viewModel, Object dyanmicFilters = null)
        {
            try
            {
                TModel model = this.Source.WhereVM(viewModel);

                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(model, viewModel, Context);

                model.MapBack(viewModel, this.Context);

                if (this.BeforeUpdate != null)
                    this.BeforeUpdate(model, viewModel, Context);

                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);

                if (AfterMap != null)
                    AfterMap(model, viewModel, Context);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                this.MapRepoFunction(viewModel);

                if (!this.Configuration.UseSecurity || this.Security.CanUpdate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterUpdate != null)
                    this.AfterUpdate(model, viewModel, Context);
                if (this.ViewModelUpdated != null)
                    ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(viewModel));

                FlushViewModelCache();
                return viewModel;
            }
            catch (Exception ex)
            {
                if (ex is ValidationException
                    || ex is DbEntityValidationException
                    || ex is DbUnexpectedValidationException)
                    throw ex;

                throw new Exception("Error Updating: " + typeof(TModel).Name, ex);
            }
        }

        public virtual IQueryable<TViewModel> Update(List<TViewModel> viewModelList, Object dynamicFilters = null)
        {
            try
            {
                foreach (var viewModel in viewModelList)
                {
                    if (this.BeforeMapBack != null)
                        this.BeforeMapBack(this.Source.WhereVM(viewModel), viewModel, Context);
                }

                var modelList = this.Source.MapBack(viewModelList, this.Context).AsQueryable();

                foreach (var viewModel in viewModelList)
                {
                    var model = modelList.WhereVM(viewModel);
                    if (this.BeforeUpdate != null)
                        this.BeforeUpdate(model, viewModel, Context);
                    if (this.Configuration.UseSecurity && !this.Security.CanUpdate(this.GetModel, viewModel))
                        throw new System.Security.SecurityException("Access to update denied.");
                }
                this.Context.SaveChanges();

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModelList, this.ImplementsICrud);

                foreach (var viewModel in viewModelList)
                {
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(modelList.WhereVM(viewModel), viewModel, Context);
                    if (this.ViewModelUpdated != null)
                        this.ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(viewModel));
                }

                FlushViewModelCache();
                var returnList = modelList.Map<TModel, TViewModel>(dynamicFilters);

                returnList.ForEach(vm =>
                {
                    if (AfterMap != null)
                        AfterMap(modelList.WhereVM(vm), vm, Context);
                    if (ViewModelMapped != null)
                        ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(vm));

                    this.MapRepoFunction(vm);
                });
                return returnList;
            }
            catch (Exception ex)
            {
                if (ex is ValidationException
                    || ex is DbEntityValidationException
                    || ex is DbUnexpectedValidationException)
                    throw ex;

                throw new Exception("Error Updating: " + typeof(TModel).Name, ex);
            }
        }

        public virtual void Delete(params Object[] ids)
        {
            try
            {
                var viewModel = this.Source.Find(this.GetTypedIDs(ids)).Map<TModel, TViewModel>();
                this.Delete(viewModel);
            }
            catch (Exception ex)
            {
                if (ex is ValidationException)
                    throw ex;

                throw new Exception("Error Deleting: " + typeof(TModel).Name, ex);
            }
        }

        public virtual void Delete(TViewModel viewModel)
        {
            try
            {

                TModel model = Source.WhereVM(viewModel);
                if (this.BeforeDelete != null)
                    this.BeforeDelete(model, viewModel, Context);
                this.Source.Remove(model);
                if (!this.Configuration.UseSecurity || this.Security.CanDelete(this.GetModel, viewModel))
                    this.Context.SaveChanges();
                else
                    throw new System.Security.SecurityException("Access to update denied.");
                if (this.AfterDelete != null)
                    this.AfterDelete(model, viewModel, Context);
                if (this.ViewModelDeleted != null)
                    this.ViewModelDeleted(this, new ViewModelEventArgs<TViewModel>(viewModel));
                FlushViewModelCache();
            }
            catch (Exception ex)
            {
                if (ex is ValidationException)
                    throw ex;

                throw new Exception("Error Deleting: " + typeof(TModel).Name, ex);
            }
        }

        [Obsolete("Create new Business Object to insure all business rules are applied")]
        public static IQueryable<TViewModel> QuickGet(Object dynamicFilters = null)
        {
            var repository = new TContext();
            var source = repository.GetIDbSet<TModel>();

            IQueryable<TViewModel> viewModels;

            viewModels = source.Map<TModel, TViewModel>(dynamicFilters);
            return viewModels;
        }

        [Obsolete("Create new Business Object to insure all business rules are applied")]
        public static TViewModel QuickGet(params object[] ids)
        {
            try
            {
                var repository = new TContext();
                var source = repository.GetIDbSet<TModel>();

                TViewModel viewModel;
                viewModel = source.Find(RepoExtentions.GetTypedIDs<TModel, TViewModel, TContext>(ids)).Map<TModel, TViewModel>();

                return viewModel;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error Getting {0} for ID: {1}", typeof(TModel).Name, ids.ToCommaDeleminatedList()), ex);
            }
        }

        public Boolean Exists(TViewModel viewModel)
        {
            return this.Source.WhereVM(viewModel) != null;
        }

        public virtual TViewModel Default()
        {
            return this.Source.Create().Map<TModel, TViewModel>();
        }

        public override void MapRepoFunction(Object viewModel, Boolean getModel = true)
        {
            if (typeof(TViewModel).IsAssignableFrom(viewModel.GetType()))
                this.MapRepoFunction((TViewModel)viewModel, getModel);
            else
                throw new Exception("The Object passed in must be derived from TViewModel");
        }

        public void MapRepoFunction(TViewModel viewModel, Boolean getModel = true)
        {
            foreach (PropertyInfo viewModelInfo in viewModel.GetType().GetProperties())
            {
                try
                {
                    var repoMap = viewModelInfo.GetCustomAttributes(typeof(RepoMappingAttribute), true).SingleOrDefault() as RepoMappingAttribute;
                    var nestRepoMap = viewModelInfo.GetCustomAttributes(typeof(NestedRepoMappingAttribute), true).SingleOrDefault() as NestedRepoMappingAttribute;
                    var allValuesMap = viewModelInfo.GetCustomAttributes(typeof(AllValuesAttribute), true).SingleOrDefault() as AllValuesAttribute;
                    var dyanmicFilterss = viewModelInfo.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() as DynamicFilterAttribute;
                    if (repoMap != null && repoMap.HasMethod)
                    {
                        viewModelInfo.SetValue(viewModel,
                            repoMap.GetMethodInfo(this, viewModel, typeof(TModel)).Invoke(this, repoMap.GetParameters(viewModel,
                            getModel ? this.GetModel : (Func<TViewModel, TModel>)null).ToArray()), null);
                    }
                    if (allValuesMap != null)
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                            if (allValuesMap.Repository != null)
                            {
                                var allValuesRepo = Repository.CreateRepo(allValuesMap.Repository, allValuesMap.Model, viewModelType, typeof(TContext));
                                viewModelInfo.SetValue(viewModel, allValuesRepo.Get(allValuesMap.Filter));
                            }
                            else
                            {
                               var allValuesList = this.Context.GetIQuery(allValuesMap.Model).Map(viewModelType).Filter(allValuesMap.Filter);
                               viewModelInfo.SetValue(viewModel, allValuesList);
                            }
                        }
                        else throw new Exception("Property Must Implement IEnumerable<>");
                    }
                    if (dyanmicFilterss != null)
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                            var viewModelList = viewModelInfo.GetValue(viewModel) as IEnumerable;
                            viewModelList = viewModelList.Filter(dyanmicFilterss.GetFilter(viewModel));

                            viewModelInfo.SetValue(viewModel, viewModelList);
                        }
                        else throw new Exception("Property Must Implement IEnumerable<>");
                    }
                    if (nestRepoMap != null)
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                            var nestedViews = ((IEnumerable)viewModelInfo.GetValue(viewModel)).Cast<Object>();
                            var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelType, typeof(TContext));
                            var list = Repository.CreateObject(typeof(List<>).MakeGenericType(viewModelType));
                            var listAddMethod = list.GetType().GetMethod("Add");
                            foreach (var nestedView in nestedViews)
                            {
                                nestRepoMap.SetParameters(viewModel, nestedView);
                                nestedViewRepo.MapRepoFunction(nestedView);
                                listAddMethod.Invoke(list, new Object[] { nestedView });
                            }

                            viewModelInfo.SetValue(viewModel, list);
                        }
                        else if (viewModelInfo.PropertyType.IsClass)
                        {
                            var nestedView = viewModelInfo.GetValue(viewModel);
                            var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelInfo.PropertyType, typeof(TContext));
                            nestRepoMap.SetParameters(viewModel, nestedView);
                            nestedViewRepo.MapRepoFunction(nestedView);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error Mapping Business Functions", ex);
                }
            }
        }

        public void Dispose()
        {
            this.Context.Dispose();
        }

        /// <summary>
        /// Sets New Key for Single Key Model
        /// If multi Key or Special Key Override Method and Set Key
        /// Returns the id
        /// </summary>
        /// <param name="viewModel">viewModel that is to have its keys set</param>
        /// <returns></returns>
        protected virtual object SetNewKey(TViewModel viewModel)
        {
            int id = this.Source.NewKey<TModel, TViewModel>();
            viewModel.SetIDs(id);
            return id;
        }

        protected virtual void FlushViewModelCache()
        {
            StaticCacheHelper.Flush(typeof(TViewModel).Name.Replace("View", "ListView"));
            StaticCacheHelper.Flush(typeof(TViewModel).Name);
        }

        protected internal TModel GetModel(TViewModel viewModel)
        {
            return this.Source.WhereVM(viewModel);
        }

        protected Boolean ImplementsICrud
        {
            get
            {
                return typeof(TViewModel).GetInterface(typeof(ICrud).Name) != null;
            }
        }

        public override void SetCrud(Object viewModel, Boolean listMode = false)
        {
            if (typeof(TViewModel).IsAssignableFrom(viewModel.GetType()))
                this.SetCrud((TViewModel)viewModel, ImplementsICrud, listMode);
            else
                throw new Exception("The Object passed in must be derived from TViewModel");
        }

        public void SetCrud(IEnumerable<TViewModel> viewModelList, Boolean iCrud, Boolean listMode = false)
        {
            foreach (var viewModel in viewModelList)
            {
                SetCrud(viewModel, iCrud, listMode);
            }
        }

        public void SetCrud(TViewModel viewModel, Boolean iCrud, Boolean listMode = false)
        {
            if (iCrud)
                Security.SetCrud(this.GetModel, viewModel, listMode);
            else
                Security.SetCrudReflection(this.GetModel, viewModel, listMode);
        }

    }

    public class ViewModelListEventArgs<TViewModel> : EventArgs
    {
        public IQueryable<TViewModel> ViewModels { get; private set; }

        public ViewModelListEventArgs(IQueryable<TViewModel> viewModels)
        {
            ViewModels = viewModels;
        }
    }

    public class ViewModelEventArgs<TViewModel> : EventArgs
    {
        public TViewModel ViewModel { get; private set; }

        public ViewModelEventArgs(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}