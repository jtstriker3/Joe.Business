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
using Joe.Reflection;
using Joe.Business.Notification;
using Joe.Business.Configuration;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Joe.Business.Approval;
using Joe.Business.Exceptions;
using Joe.Caching;

namespace Joe.Business
{

    public abstract class Repository : IRepository
    {
        public abstract void SetCrud(Object viewModel, Boolean listMode = false);
        public abstract void MapRepoFunction(Object viewModel, Boolean getModel = true);
        public abstract IEnumerable Get(String filter = null);
        public abstract IDBViewContext CreateContext();
        public static ISecurityFactory _securityFactory;
        protected internal abstract IDBViewContext Context { get; set; }
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
            var repoInterface = RepositoryType.GetInterface("IRepository`2");
            var model = repoInterface.GetGenericArguments().First();
            var viewModel = repoInterface.GetGenericArguments().ToArray()[1];
            var genericRepo = typeof(Repository<,>).MakeGenericType(model, viewModel);
            var method = genericRepo.GetMethod("CreateRepo");
            var repo = (IRepository)method.Invoke(null, new Object[] { RepositoryType });

            return repo;
        }
        public static IRepository CreateRepo(Type RepositoryType, Type model, Type viewModel)
        {
            var genericRepo = typeof(Repository<,>).MakeGenericType(model, viewModel);
            var method = genericRepo.GetMethod("CreateRepo");
            var repo = (IRepository)method.Invoke(null, new Object[] { RepositoryType });

            return repo;
        }
        protected internal static Object CreateObject(Type type)
        {

            var newExpression = Expression.New(type);
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return lambdaExpression.Compile().DynamicInvoke();
        }

        internal IEnumerable<String> GetIncludeMappings { get; set; }
        protected static Dictionary<String, Object> FilterDictionary = new Dictionary<string, object>();
        internal void LogFilter(Object filter)
        {
            if (filter != null)
            {
                var key = this.GetFilterKey(filter);

                if (!FilterDictionary.ContainsKey(key))
                    FilterDictionary.Add(key, filter);
            }
        }
        protected abstract String GetFilterKey(Object filter);
        public void Dispose()
        {
            this.Context.Dispose();
        }

    }

    public abstract class Repository<TModel, TViewModel> : Repository, IRepository<TModel, TViewModel>
        where TModel : class
        where TViewModel : class, new()
    {
        private IPersistenceSet<TModel> _source;
        protected virtual IPersistenceSet<TModel> Source
        {
            get
            {
                return _source ?? this.Context.GetIPersistenceSet<TModel>();
            }
            set { _source = value; }
        }
        protected BusinessConfigurationAttribute Configuration { get; set; }
        protected virtual ISecurity<TModel> Security { get; set; }
        protected INotificationProvider NotificationProvider { get; set; }
        protected IEmailProvider EmailProvider { get; set; }
        private IDBViewContext _context;
        protected internal override IDBViewContext Context
        {
            get
            {
                _context = _context ?? this.CreateContext();
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        #region Delegates
        protected delegate void MapDelegate(MapDelegateArgs<TModel, TViewModel> mapDelegateArgs);
        protected delegate void SaveDelegate(SaveDelegateArgs<TModel, TViewModel> saveDelegateArgs);
        protected delegate void AfterGetDelegate(AfterGetDelegateArgs<TViewModel> afterGetDelegateArgs);
        protected delegate void BeforeGetDelegate();
        protected delegate IQueryable<TViewModel> GetListDelegate(GetListDelegateArgs<TViewModel> getListDelegateArgs);
        protected SaveDelegate BeforeUpdate;
        protected SaveDelegate BeforeDelete;
        protected SaveDelegate BeforeCreate;
        protected SaveDelegate AfterUpdate;
        protected SaveDelegate AfterDelete;
        /// <summary>
        /// When Creating an object SaveChanges is called twice
        /// The First Saves just the focused entity
        /// The Second saves the nested List attached to the focused Entity
        /// </summary>
        protected SaveDelegate BeforeCreateInitialSave;
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

        protected String CacheKey
        {
            get
            {
                return typeof(TViewModel).FullName + typeof(TModel).FullName;
            }
        }
        protected String ListCacheKey
        {
            get
            {
                return typeof(TViewModel).FullName + typeof(TModel).FullName + "List";
            }
        }

        public Repository(ISecurity<TModel> security, IDBViewContext repositiory)
        {
            Configuration = (BusinessConfigurationAttribute)GetType().GetCustomAttributes(typeof(BusinessConfigurationAttribute), true).SingleOrDefault() ?? new BusinessConfigurationAttribute();
            Context = repositiory;
            Security = security ?? this.TryGetSecurityForModel();
            NotificationProvider = Joe.Business.Notification.NotificationProvider.ProviderInstance;
            EmailProvider = FactoriesAndProviders.EmailProvider;
            var includeAttribute = typeof(TViewModel).GetCustomAttribute<IncludeAttribute>();
            GetIncludeMappings = includeAttribute != null ? includeAttribute.IncludeMappings : new List<String>();
        }

        public Repository(ISecurity<TModel> security)
            : this(security, null)
        {

        }

        public Repository(IDBViewContext repositiory)
            : this(new Security<TModel>(), repositiory)
        {

        }

        public Repository() :
            this(null, null)
        {

        }

        public virtual Result<TViewModel> Create(TViewModel viewModel, Object dynamicFilters = null)
        {
            try
            {
                var result = new Result<TViewModel>(viewModel);
                var saved = false;
                if (typeof(TModel).IsAbstract)
                    throw new Exception("You cannot Create Abstract Model Objects...Duh");

                var model = Source.Create();
                var prestineModel = Source.Create();
                if (Configuration.IncrementKey)
                    SetNewKey(viewModel);


                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));

                model = this.Source.Add(model);
                model.MapBack(viewModel, this.Context, () =>
                {
                    if (!this.Configuration.EnforceSecurity || this.Security.CanCreate( vm => model, viewModel, false))
                    {
                        if (this.BeforeCreateInitialSave != null)
                            this.BeforeCreateInitialSave(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));
                        Context.SaveChanges();
                        saved = true;
                    }
                    else
                        throw new System.Security.SecurityException("Access to update denied.");
                });

                try
                {
                    if (this.BeforeCreate != null)
                        this.BeforeCreate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));
                }
                catch (Exception ex)
                {
                    //Delete Model that is saved to database during the initial save but then has some sort of post initial save exception
                    if (saved)
                    {
                        try
                        {
                            viewModel = model.Map<TModel, TViewModel>();
                            this.Delete(viewModel);
                            this.Context.SaveChanges();
                        }
                        catch
                        {
                            //Do Nothing
                        }
                    }

                    throw ex;
                }
                if (!this.Configuration.EnforceSecurity || this.Security.CanCreate(this.GetModel, viewModel, false))
                {
                    this.Context.SaveChanges();
                    FlushListCache(typeof(TModel));
                    viewModel = model.Map<TModel, TViewModel>(dynamicFilters);
                    result.ViewModel = viewModel;

                    this.LogFilter(dynamicFilters);

                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Create, model, null, this.EmailProvider);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);


                if (this.AfterCreate != null)
                    this.AfterCreate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));
                if (this.ViewModelCreated != null)
                    this.ViewModelCreated(this, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Create));

                return result;
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
            //Stop get by id from being called when just getting a list with no optional parameters
            return Get(filter: null);
        }

        public virtual IQueryable<TViewModel> Get(
            Expression<Func<TViewModel, Boolean>> filter = null,
            Expression<Func<TModel, Boolean>> sourceFilter = null,
            int? take = null,
            int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapRepoFunctionsOverride = true,
            Boolean descending = false,
            String stringfilter = null,
            Object dyanmicFilter = null,
            params String[] orderBy)
        {
            int count;
            return Get(out count, filter, sourceFilter, take, skip, setCrudOverride, mapRepoFunctionsOverride, descending, stringfilter, dyanmicFilter, false, orderBy);
        }

        /// <summary>
        /// Get a list of ViewModels
        /// </summary>
        /// <returns>Returns a list of ViewModesl. If UseSecurity and SetCrud are set to true then the list will be filtered to only return results the user has access too.</returns>
        public virtual IQueryable<TViewModel> Get(
            out int count,
            Expression<Func<TViewModel, Boolean>> filter = null,
            Expression<Func<TModel, Boolean>> sourceFilter = null,
            int? take = null, int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapRepoFunctionsOverride = true,
            Boolean descending = false,
            String stringfilter = null,
            Object dyanmicFilters = null,
            Boolean setCount = true,
            params String[] orderBy
            )
        {
            if (!this.Configuration.EnforceSecurity || this.Security.HasReadRights<TViewModel>())
            {
                Boolean inMemory = false;
                IQueryable<TViewModel> viewModels;
                IQueryable<TViewModel> source;
                if ((Configuration.GetListFromCache || CacheAttribute.HasCacheAttribute<TViewModel>()))
                {
                    IQueryable<TViewModel> cachedViewModels;
                    String listCacheKey = ListCacheKey;
                    //Must Add the user ID to cache key becuase it could be different for each user
                    if (this.Configuration.EnforceSecurity && this.Security.GetType() != typeof(Security<TModel>))
                        listCacheKey += this.Security.ProviderInstance.UserID;

                    cachedViewModels = Joe.Caching.Cache.Instance.GetOrAdd(listCacheKey, new TimeSpan(BusinessConfigurationSection.Instance.CacheDuration, 0, 0), GetCachedList, this.Source, dyanmicFilters, sourceFilter, this.Configuration, this.Security);

                    source = CopyList(cachedViewModels).AsQueryable();
                }
                else
                {
                    IQueryable<TModel> modelSource;
                    if (this.Configuration.EnforceSecurity)
                        modelSource = this.Security.SecureList(this.Source);
                    else
                        modelSource = this.Source;

                    if (sourceFilter != null)
                        source = modelSource.Where(sourceFilter).Map<TModel, TViewModel>(dyanmicFilters);
                    else
                        source = modelSource.Map<TModel, TViewModel>(dyanmicFilters);
                }

                viewModels = source;

                if (filter != null)
                    viewModels = viewModels.Where(filter);
                if (!String.IsNullOrEmpty(stringfilter))
                    viewModels = viewModels.Filter(stringfilter);
                if (setCount && this.Configuration.SetCount || take.HasValue)
                    count = viewModels.Count();
                else
                    count = 0;

                if (orderBy.Count() > 0 && orderBy.First() != null)
                {
                    if (!descending)
                        viewModels = viewModels.OrderBy(orderBy);
                    else
                        viewModels = viewModels.OrderByDescending(orderBy);
                }
                else if (skip.HasValue)
                {
                    if (!typeof(IOrderedEnumerable<>).IsAssignableFrom(viewModels.GetType().GetGenericTypeDefinition())
                        && !typeof(IOrderedQueryable).IsAssignableFrom(viewModels.GetType()))
                    {
                        var defatulOrderBy = typeof(TViewModel).GetProperties().Where(prop => prop.PropertyType.IsSimpleType()
                            && new ViewMappingHelper(prop, typeof(TModel)).ViewMapping != null).FirstOrDefault();

                        if (defatulOrderBy != null)
                            viewModels = viewModels.OrderBy(defatulOrderBy.Name);
                    }
                }
                if (skip.HasValue)
                    viewModels = viewModels.Skip(skip.Value);
                if (take.HasValue)
                    viewModels = viewModels.Take(take.Value);

                if (this.Configuration.SetCrud && setCrudOverride)
                {
                    var viewModelList = viewModels.ToList();
                    inMemory = true;
                    this.SetCrud(viewModelList, this.ImplementsICrud);
                    viewModels = viewModelList.AsQueryable();
                }

                if (ViewModelListMapped != null)
                    viewModels = ViewModelListMapped(this, new ViewModelListEventArgs<TViewModel>(viewModels));

                if (this.Configuration.MapRepositoryFunctionsForList && mapRepoFunctionsOverride)
                {
                    if (!inMemory)
                        //Load the list into memory so any changes are saved to the List returned to calling function
                        viewModels = viewModels.ToList().AsQueryable();
                    viewModels.ForEach(vm => this.MapRepoFunction(vm, true));
                }

                if (this.BeforeReturnList != null)
                    viewModels = this.BeforeReturnList(new GetListDelegateArgs<TViewModel>(viewModels));
                if (this.ViewModelListRetrieved != null)
                    viewModels = ViewModelListRetrieved(this, new ViewModelListEventArgs<TViewModel>(viewModels));

                return viewModels;
            }

            throw new UnauthorizedAccessException("You do not have Read Access!");
        }

        public override IEnumerable Get(string filter = null)
        {
            int count = 0;
            return this.Get(out count, stringfilter: filter, setCrudOverride: false, mapRepoFunctionsOverride: false);
        }

        private static IQueryable<TViewModel> GetCachedList([DoNotHash]IQueryable<TModel> list, object dyFilters, Expression<Func<TModel, Boolean>> sFilter, BusinessConfigurationAttribute configuration, [DoNotHash]ISecurity<TModel> security)
        {
            IQueryable<TModel> modelSource;
            IQueryable<TViewModel> source;
            if (configuration.EnforceSecurity)
                modelSource = security.SecureList(list);
            else
                modelSource = list;

            if (sFilter != null)
                source = modelSource.Where(sFilter).Map<TModel, TViewModel>(dyFilters);
            else
                source = modelSource.Map<TModel, TViewModel>(dyFilters);

            return source.ToList().AsQueryable();
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
                CachedResult<TModel, TViewModel> cachedResults = GetCachedResult(dyanmicFilters, ids, this.Context);

                if (cachedResults != null)
                {
                    var viewModel = new TViewModel();
                    Joe.Reflection.ReflectionHelper.RefelectiveMap(cachedResults.ViewModel, viewModel);

                    var model = this.Source.Local.AsQueryable().Find<TModel, TViewModel>(false, this.GetTypedIDs(ids));
                    if (model == null)
                    {
                        model = cachedResults.Model;
                        //this.Source.Attach(model);
                    }

                    //viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);

                    //this.MapRepoFunction(viewModel);

                    if (this.Configuration.SetCrud && setCrud)
                        this.SetCrud(viewModel, this.ImplementsICrud);

                    if (this.AfterGet != null)
                        this.AfterGet(new AfterGetDelegateArgs<TViewModel>(viewModel));
                    if (!this.Configuration.EnforceSecurity || this.Security.CanRead(this.GetModel, viewModel, false))
                    {
                        if (this.ViewModelRetrieved != null)
                            this.ViewModelRetrieved(this, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Get));
                        if (this.NotificationProvider != null)
                            this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Read, model, null, this.EmailProvider);
                        //this.Context.ObjectContext.Detach(model);
                        return viewModel;
                    }
                    else
                    {
                        //this.Context.ObjectContext.Detach(model);
                        throw new System.Security.SecurityException(String.Format("Access to read denied for: {0}", typeof(TModel).Name));
                    }

                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error Getting: " + typeof(TModel).Name, ex);
            }
        }

        public virtual Result<TViewModel> Update(TViewModel viewModel, Object dynamicFilters = null)
        {
            return this.Update(viewModel, false, dynamicFilters);
        }

        public virtual Result<TViewModel> Update(TViewModel viewModel, Boolean overrideApproval, Object dynamicFilters = null)
        {
            TModel model = null;
            try
            {
                var result = new Result<TViewModel>(viewModel);
                if (!overrideApproval)
                {
                    var approvalProvider = Approval.ApprovalProvider.Instance;
                    if (approvalProvider != null)
                    {
                        var approvalResult = approvalProvider.ProcessApproval(typeof(TModel), this.GetType(), viewModel);
                        if (approvalResult.HasValue)
                            throw new ApprovalNeededException(approvalResult.Value);
                    }
                }

                if (this.Configuration.IncludeMappings.Count() > 0)
                    model = this.Source.BuildIncludeMappings(this.Configuration.IncludeMappings.ToArray()).WhereVM(viewModel);
                else
                    model = this.Source.WhereVM(viewModel);
                var prestineModel = model.ShallowClone();

                this.LogFilter(dynamicFilters);

                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));

                model.MapBack(viewModel, this.Context);

                if (this.BeforeUpdate != null)
                    this.BeforeUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));

                if (!this.Configuration.EnforceSecurity || this.Security.CanUpdate(this.GetModel, viewModel, false))
                {
                    this.Context.SaveChanges();
                    //See if there are any notifications for Model
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Update, model, prestineModel, this.EmailProvider);
                    //See if a History Record Needs to be Saved for Model Type
                    if (History.HistoryProvider.Instance != null)
                        History.HistoryProvider.Instance.ProcessHistory(model);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                viewModel = model.Map<TModel, TViewModel>(dynamicFilters);
                result.ViewModel = viewModel;

                if (AfterMap != null)
                    AfterMap(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Update));

                this.MapRepoFunction(viewModel);

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterUpdate != null)
                    this.AfterUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));
                if (this.ViewModelUpdated != null)
                    ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Update));

                FlushSingleItemAndListCache(viewModel, model, dynamicFilters, true);

                return result;
            }
            catch (Exception ex)
            {
                if (model != null)
                    FlushSingleItemAndListCache(viewModel, model, dynamicFilters, true);
                if (ex is ValidationException
                    || ex is DbEntityValidationException
                    || ex is DbUnexpectedValidationException
                    || ex is ApprovalNeededException)
                    throw ex;

                throw new Exception("Error Updating: " + typeof(TModel).Name, ex);
            }
        }

        public virtual IEnumerable<Result<TViewModel>> Update(List<TViewModel> viewModelList, Object dynamicFilters = null)
        {
            try
            {
                //foreach (var viewModel in viewModelList)
                //{

                //}

                List<Tuple<TModel, TModel, TViewModel, Result<TViewModel>>> modelPrestineModelViewModelList = new List<Tuple<TModel, TModel, TViewModel, Result<TViewModel>>>();
                foreach (var viewModel in viewModelList)
                {
                    var result = new Result<TViewModel>(viewModel);
                    var model = this.Source.WhereVM(viewModel);
                    var prestineModel = model.ShallowClone();

                    if (this.BeforeMapBack != null)
                        this.BeforeMapBack(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));

                    model.MapBack(viewModel, this.Context);

                    if (this.BeforeUpdate != null)
                        this.BeforeUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel, result));
                    if (this.Configuration.EnforceSecurity && !this.Security.CanUpdate(this.GetModel, viewModel, false))
                        throw new System.Security.SecurityException("Access to update denied.");

                    modelPrestineModelViewModelList.Add(new Tuple<TModel, TModel, TViewModel, Result<TViewModel>>(model, prestineModel, viewModel, result));
                }

                this.Context.SaveChanges();

                if (this.NotificationProvider != null)
                    foreach (var modelTuple in modelPrestineModelViewModelList)
                    {
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Update, modelTuple.Item1, modelTuple.Item2, this.EmailProvider);
                    }

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModelList, this.ImplementsICrud);

                foreach (var modelTuple in modelPrestineModelViewModelList)
                {
                    FlushSingleItemAndListCache(modelTuple.Item3, modelTuple.Item1, dynamicFilters, true);
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(new SaveDelegateArgs<TModel, TViewModel>(modelTuple.Item1, modelTuple.Item2, modelTuple.Item3, modelTuple.Item4));
                    if (this.ViewModelUpdated != null)
                        this.ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(modelTuple.Item3, OperationType.Update));
                }

                var modelList = modelPrestineModelViewModelList.Select(modelTuple => modelTuple.Item1);
                var resultList = modelPrestineModelViewModelList.Select(modeltuple => modeltuple.Item4);
                var returnList = modelList.Map<TModel, TViewModel>(dynamicFilters).ToList();

                returnList.ForEach(vm =>
                {
                    if (AfterMap != null)
                        AfterMap(new MapDelegateArgs<TModel, TViewModel>(modelList.AsQueryable().WhereVM(vm), vm));
                    if (ViewModelMapped != null)
                        ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(vm, OperationType.Update));

                    this.MapRepoFunction(vm);

                    var result = resultList.First(r => r.ViewModel.GetIDs().ToCommaDeleminatedList() == vm.GetIDs().ToCommaDeleminatedList());

                    result.ViewModel = vm;

                });
                return resultList;
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
                var result = new Result<TViewModel>(viewModel);
                TModel model = Source.WhereVM(viewModel);
                if (this.BeforeDelete != null)
                    this.BeforeDelete(new SaveDelegateArgs<TModel, TViewModel>(model, model, viewModel, result));

                if (!this.Configuration.EnforceSecurity || this.Security.CanDelete(this.GetModel, viewModel, false))
                {
                    this.Source.Remove(model);
                    this.Context.SaveChanges();
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Delete, model, null, this.EmailProvider);

                    var getModelArgs = new GetModelArgs<TModel, TViewModel>(viewModel.GetIDs().ToArray(), null, this);
                    this.FlushSingleItemAndListCache(viewModel, model, null);
                }
                else
                    throw new System.Security.SecurityException("Access to delete denied.");
                if (this.AfterDelete != null)
                    this.AfterDelete(new SaveDelegateArgs<TModel, TViewModel>(model, model, viewModel, result));
                if (this.ViewModelDeleted != null)
                    this.ViewModelDeleted(this, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Delete));
            }
            catch (Exception ex)
            {
                if (ex is ValidationException)
                    throw ex;

                throw new Exception("Error Deleting: " + typeof(TModel).Name, ex);
            }
        }

        public Boolean Exists(TViewModel viewModel)
        {
            return this.Source.WhereVM(viewModel) != null;
        }

        public Boolean Exists(params Object[] ids)
        {
            return this.Source.Find(this.GetTypedIDs(ids)) != null;
        }

        public virtual TViewModel Default(TViewModel defaultValues = null)
        {
            if (typeof(TModel).IsAbstract)
                throw new Exception("You cannot Create Abstract Model Objects...Duh");
            var model = this.Source.Create();
            TViewModel viewModel;
            if (defaultValues != null)
                model.MapBack(defaultValues);
            else
                defaultValues = model.Map<TModel, TViewModel>();

            var keyTypes = RepoExtentions.GetKeyInfo<TViewModel, TModel>(defaultValues);
            var nullKeys = keyTypes.Where(key => key.Item2 == null).Select(key => key.Item1.PropertyType);
            if (!nullKeys.Contains(typeof(string)) && !nullKeys.Contains(typeof(Guid)))
            {
                this.Source.Attach(model);
                viewModel = model.Map<TModel, TViewModel>();
                this.Context.Detach(model);
            }
            else
                viewModel = model.Map<TModel, TViewModel>();

            return viewModel;
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

        protected internal TModel GetModel(TViewModel viewModel)
        {
            return this.Source.WhereVM(viewModel);
        }

        public override IDBViewContext CreateContext()
        {
            if (FactoriesAndProviders.ContextFactory != null)
                return FactoriesAndProviders.ContextFactory.CreateContext<TModel>();

            throw new ContextFactoryNotSetException();
        }

        #region Eval Attributes

        public override void MapRepoFunction(Object viewModel, Boolean getModel = true)
        {
            if (typeof(TViewModel).IsAssignableFrom(viewModel.GetType()))
                this.MapRepoFunction((TViewModel)viewModel, getModel);
            else
                throw new Exception("The Object passed in must be derived from TViewModel");
        }

        public void MapRepoFunction(TViewModel viewModel, Boolean getModel = true)
        {
            this.MapRepoFunction(viewModel, false, getModel);
        }

        public void MapRepoFunction(TViewModel viewModel, Boolean isList, Boolean getModel = true)
        {
            var key = typeof(TViewModel).FullName + "EvalFunctions";

            Delegate getPropsDelegate = (Func<IEnumerable<PropertyInfo>>)(() =>
            {
                return GetPropertiesToEval();
            });

            var properties = (IEnumerable<PropertyInfo>)Joe.Caching.Cache.Instance.GetOrAdd(key, TimeSpan.MaxValue, getPropsDelegate);

            foreach (PropertyInfo viewModelInfo in properties)
            {
                try
                {
                    var viewModelAsList = new List<TViewModel>() { viewModel }.AsQueryable();
                    var dynamicFilterApplied = false;
                    var repoMap = viewModelInfo.GetCustomAttributes(typeof(RepoMappingAttribute), true).SingleOrDefault() as RepoMappingAttribute;
                    var nestRepoMap = viewModelInfo.GetCustomAttributes(typeof(NestedRepoMappingAttribute), true).SingleOrDefault() as NestedRepoMappingAttribute;
                    var allValuesMap = viewModelInfo.GetCustomAttributes(typeof(AllValuesAttribute), true).SingleOrDefault() as AllValuesAttribute;
                    var dyanmicFilters = viewModelInfo.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() as DynamicFilterAttribute;


                    if (nestRepoMap != null)
                    {
                        if (this.ConditionTrue(viewModelAsList, nestRepoMap.Condition))
                        {
                            if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                            {

                                var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                                var nestedViewAsObeject = viewModelInfo.GetValue(viewModel);
                                if (nestedViewAsObeject != null)
                                {
                                    var nestedViews = ((IEnumerable)nestedViewAsObeject).Cast<Object>();
                                    var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelType);
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

                            }
                            else if (viewModelInfo.PropertyType.IsClass)
                            {
                                var nestedView = viewModelInfo.GetValue(viewModel);
                                var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelInfo.PropertyType);
                                nestRepoMap.SetParameters(viewModel, nestedView);
                                nestedViewRepo.MapRepoFunction(nestedView);
                            }
                        }
                    }
                    if (repoMap != null && repoMap.HasMethod)
                    {
                        if (this.ConditionTrue(viewModelAsList, repoMap.Condition))
                        {
                            Object methodObj = null;

                            if (repoMap.HelperClass != null)
                                methodObj = Repository.CreateObject(repoMap.HelperClass);
                            else
                                methodObj = this;

                            viewModelInfo.SetValue(viewModel,
                                repoMap.GetMethodInfo(this, viewModel, typeof(TModel)).Invoke(methodObj, repoMap.GetParameters(viewModel,
                                getModel ? this.GetModel : (Func<TViewModel, TModel>)null).ToArray()), null);
                        }
                    }
                    if (allValuesMap != null
                        && (!isList
                        || allValuesMap.SetForList))
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            if (this.ConditionTrue(viewModelAsList, allValuesMap.Condition))
                            {
                                var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                                IEnumerable allValuesList;
                                if (allValuesMap.Repository != null)
                                    allValuesList = Repository.CreateRepo(allValuesMap.Repository, allValuesMap.Model, viewModelType).Get(allValuesMap.Filter);
                                else
                                {
                                    //var method = typeof(IDBViewContext).GetMethods().Single(m => m.Name == "GetIPersistenceSet" && m.IsGenericMethod);
                                    //method = method.MakeGenericMethod(allValuesMap.Model);
                                    //allValuesList = ((IEnumerable)method.Invoke(this.Context, null)).Map(allValuesMap.Model, viewModelType).Filter(allValuesMap.Filter);
                                    allValuesList = this.Context.GetGenericQueryable(allValuesMap.Model).Map(allValuesMap.Model, viewModelType, null).Filter(allValuesMap.Filter);
                                }

                                var list = Repository.CreateObject(typeof(List<>).MakeGenericType(viewModelType));
                                var listAddMethod = list.GetType().GetMethod("Add");
                                if (!String.IsNullOrWhiteSpace(allValuesMap.IncludedList))
                                {
                                    var includedValues = ReflectionHelper.GetEvalProperty(viewModel, allValuesMap.IncludedList) as IEnumerable;

                                    //Set the Value before we apply dynamic filters
                                    viewModelInfo.SetValue(viewModel, allValuesList);
                                    ApplyDynamicFilters(viewModel, viewModelInfo, viewModelAsList, dyanmicFilters);
                                    dynamicFilterApplied = true;
                                    allValuesList = (IEnumerable)viewModelInfo.GetValue(viewModel);

                                    if (includedValues != null)
                                    {
                                        var includedPropertyInfo = ReflectionHelper.TryGetEvalPropertyInfo(viewModel.GetType(), allValuesMap.IncludedList);
                                        var genericType = includedPropertyInfo.PropertyType.GetGenericArguments().Single();
                                        foreach (var item in allValuesList)
                                        {
                                            if (includedValues != null)
                                                if (includedValues.WhereVM(item, genericType) != null)
                                                    ReflectionHelper.SetEvalProperty(item, "Included", true);

                                            listAddMethod.Invoke(list, new Object[] { item });
                                        }
                                        allValuesList = (IEnumerable)list;
                                    }
                                }
                                viewModelInfo.SetValue(viewModel, allValuesList);
                            }
                        }
                        else throw new Exception("Property Must Implement IEnumerable<>");
                    }
                    if (!dynamicFilterApplied)
                        ApplyDynamicFilters(viewModel, viewModelInfo, viewModelAsList, dyanmicFilters);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error Mapping Business Functions For Property {0} in Class {1}", viewModelInfo.Name, viewModelInfo.DeclaringType.Name), ex);
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetPropertiesToEval()
        {
            IEnumerable<PropertyInfo> properties = null;
            properties = typeof(TViewModel).GetProperties().Where(prop =>
                                        prop.GetCustomAttributes(typeof(RepoMappingAttribute), true).SingleOrDefault() != null
                                        || prop.GetCustomAttributes(typeof(NestedRepoMappingAttribute), true).SingleOrDefault() != null
                                        || prop.GetCustomAttributes(typeof(AllValuesAttribute), true).SingleOrDefault() != null
                                        || prop.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() != null).ToList();

            return properties;
        }

        private void ApplyDynamicFilters(TViewModel viewModel, PropertyInfo viewModelInfo, IQueryable<TViewModel> viewModelAsList, DynamicFilterAttribute dyanmicFilters)
        {
            if (dyanmicFilters != null)
            {
                if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                {
                    if (this.ConditionTrue(viewModelAsList, dyanmicFilters.Condition))
                    {
                        var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                        var viewModelList = viewModelInfo.GetValue(viewModel) as IEnumerable;
                        viewModelList = viewModelList.Filter(dyanmicFilters.GetFilter(viewModel));

                        viewModelInfo.SetValue(viewModel, viewModelList);
                    }
                }
                else throw new Exception("Property Must Implement IEnumerable<>");
            }
        }

        private Boolean ConditionTrue(IQueryable<TViewModel> viewModelAsList, String conditon)
        {
            if (!String.IsNullOrWhiteSpace(conditon))
                return viewModelAsList.Filter(conditon, viewModelAsList.Single()).Count() > 0;

            return true;
        }

        #endregion

        #region Security Helpers

        protected Boolean ImplementsICrud
        {
            get
            {
                return typeof(TViewModel).GetInterface(typeof(ICrud).Name) != null;
            }
        }

        public override void SetCrud(Object viewModel, Boolean listMode)
        {
            if (typeof(TViewModel).IsAssignableFrom(viewModel.GetType()))
                this.SetCrud((TViewModel)viewModel, ImplementsICrud, false);
            else
                throw new Exception("The Object passed in must be derived from TViewModel");
        }

        public void SetCrud(IEnumerable<TViewModel> viewModelList, Boolean iCrud)
        {
            foreach (var viewModel in viewModelList)
            {
                SetCrud(viewModel, iCrud, true);
            }
        }

        public void SetCrud(TViewModel viewModel, Boolean iCrud, bool forList)
        {
            if (iCrud)
                Security.SetCrud(this.GetModel, viewModel, forList);
            else
                Security.SetCrudReflection(this.GetModel, viewModel, forList);
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

        #endregion

        #region Caching Helpers

        protected IEnumerable<TViewModel> CopyList(IEnumerable<TViewModel> list)
        {
            foreach (var item in list)
            {
                var newViewModel = new TViewModel();
                Joe.Reflection.ReflectionHelper.RefelectiveMap(item, newViewModel);
                yield return newViewModel;
            }
        }

        protected virtual void FlushSingleItemAndListCache(TViewModel viewModel, TModel model, Object dynamicFilters)
        {
            FlushSingleItemAndListCache(viewModel, model, dynamicFilters, false);
        }

        protected virtual void FlushSingleItemAndListCache(TViewModel viewModel, TModel model, Object dynamicFilters, Boolean error)
        {
            var ids = viewModel.GetIDs().ToArray();
            var getModelArgs = new GetModelArgs<TModel, TViewModel>(ids, dynamicFilters, this);
            var cacheResult = (CachedResult<TModel, TViewModel>)Caching.Cache.Instance.Get(CacheKey, getModelArgs);
            if (cacheResult != null)
                cacheResult.Update(model, viewModel);

            FlushTypeByFullName(typeof(TModel), dynamicFilters, error, ids);

        }

        protected void FlushTypeByFullName(Type modelType, Object dynamicFilters = null, params Object[] ids)
        {
            FlushTypeByFullName(modelType, dynamicFilters, true, ids);
        }

        private void FlushTypeByFullName(Type modelType, Object dynamicFilters, Boolean flushNullFilter, params Object[] ids)
        {
            //Fire off in a new thread to user does not have to wait for what might be a large loop
            Action<Object[], Type> clearFilters = (Object[] modelIDs, Type type) =>
            {
                var filters = FilterDictionary.Where(filter => filter.Key.Contains(type.FullName));
                //Loop Through All Filters passed in for this model type and clear the cached value
                foreach (var filter in filters)
                {
                    var getModelArgs = new GetModelArgs<TModel, TViewModel>(modelIDs, filter, this);
                    Joe.Caching.Cache.Instance.FlushMany(type.FullName, CacheKey, getModelArgs);
                }
                if (flushNullFilter)
                {
                    var getModelArgs = new GetModelArgs<TModel, TViewModel>(modelIDs, null, this);
                    Joe.Caching.Cache.Instance.FlushMany(type.FullName);
                }
            };
            FlushListCache(modelType);
            Task.Factory.StartNew(() => clearFilters(ids, modelType));
        }

        protected virtual void FlushListCache()
        {
            this.FlushListCache(typeof(TModel));
        }

        protected virtual void FlushListCache(Type modelType)
        {
            Joe.Caching.Cache.Instance.FlushMany(modelType.FullName + "List");
        }

        private static CachedResult<TModel, TViewModel> GetCachedResultDelegate(GetModelArgs<TModel, TViewModel> args)
        {
            //var context = this.CreateContext();
            TModel model;
            if (args.Repository.GetIncludeMappings.Count() > 0)
                model = args.Repository.Context.GetIPersistenceSet<TModel>().BuildIncludeMappings(args.Repository.GetIncludeMappings.ToArray()).Find<TModel, TViewModel>(true, RepoExtentions.GetTypedIDs<TViewModel>(args.Ids));
            else
                model = args.Repository.Context.GetIPersistenceSet<TModel>().Find(RepoExtentions.GetTypedIDs<TViewModel>(args.Ids));

            if (model != null)
            {
                //The First BO will be long lived. Since the context is disposed we must reset it;- Not the case now that the Repo is being passed in as an argument
                //args.Repository.Context = args.Context;
                var viewModel = model.Map<TModel, TViewModel>(args.Filters);
                args.Repository.LogFilter(args.Filters);

                //Fire Off Events Before Mapping of Repo Functions
                if (args.Repository.AfterMap != null)
                    args.Repository.AfterMap(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));
                if (args.Repository.ViewModelMapped != null)
                    args.Repository.ViewModelMapped(args.Repository, new ViewModelEventArgs<TViewModel>(viewModel, OperationType.Get));

                args.Repository.MapRepoFunction(viewModel);
                args.Repository.Context.Detach(model);
                return new CachedResult<TModel, TViewModel>(model, viewModel);
            }
            return null;

        }

        private CachedResult<TModel, TViewModel> GetCachedResult(Object dyanmicFilters, Object[] ids, IDBViewContext context)
        {
            var cacheTimeout = this.Configuration.UseCacheForSingleItem ? Joe.Business.Configuration.BusinessConfigurationSection.Instance.CacheDuration : 0;
            CachedResult<TModel, TViewModel> cachedResults;

            if (this.BeforeGet != null)
                this.BeforeGet();
            //if (this.Configuration.GetListFromCache)
            //{
            var modelIDs = new GetModelArgs<TModel, TViewModel>(this.GetTypedIDs(ids), dyanmicFilters, this);
            cachedResults = (CachedResult<TModel, TViewModel>)Joe.Caching.Cache.Instance.Get(CacheKey, modelIDs);

            if (cachedResults == null)
            {
                Delegate getCachedModel = (Func<GetModelArgs<TModel, TViewModel>, CachedResult<TModel, TViewModel>>)((GetModelArgs<TModel, TViewModel> args) =>
                {
                    return GetCachedResultDelegate(args);
                });
                Joe.Caching.Cache.Instance.Add(CacheKey, new TimeSpan(cacheTimeout, 0, 0), getCachedModel);
                cachedResults = (CachedResult<TModel, TViewModel>)Joe.Caching.Cache.Instance.Get(CacheKey, modelIDs);
            }
            return cachedResults;
        }

        protected override String GetFilterKey(Object filter)
        {
            return typeof(TModel).FullName + filter.GetHashCode();
        }

        #endregion

        public new static IRepository<TModel, TViewModel> CreateRepo(Type RepositoryType)
        {
            IRepository<TModel, TViewModel> repo = null;
            if (RepositoryType.IsGenericType)
            {
                if (RepositoryType.GetGenericArguments().Count() == 2)
                    repo = (IRepository<TModel, TViewModel>)CreateObject(RepositoryType.MakeGenericType(typeof(TModel), typeof(TViewModel)));
                else if (RepositoryType.GetGenericArguments().Count() == 1)
                    repo = (IRepository<TModel, TViewModel>)CreateObject(RepositoryType.MakeGenericType(typeof(TViewModel)));
                else
                    repo = (IRepository<TModel, TViewModel>)CreateObject(RepositoryType.MakeGenericType(typeof(TModel), typeof(TViewModel)));
            }
            else
                repo = (IRepository<TModel, TViewModel>)CreateObject(RepositoryType);


            return repo;
        }
    }

    //public abstract class Repository<TModel, TViewModel, TContext> : Repository<TModel, TViewModel>, IRepository<TModel, TViewModel, TContext>
    //    where TModel : class
    //    where TViewModel : class, new()
    //    where TContext : IDBViewContext, new()
    //{
    //    public new static IRepository<TModel, TViewModel, TContext> CreateRepo(Type RepositoryType)
    //    {
    //        IRepository<TModel, TViewModel, TContext> repo = null;
    //        if (RepositoryType.IsGenericType)
    //        {
    //            if (RepositoryType.GetGenericArguments().Count() == 2)
    //                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TViewModel), typeof(TContext)));
    //            else if (RepositoryType.GetGenericArguments().Count() == 1)
    //                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TViewModel)));
    //            else
    //                repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TModel), typeof(TViewModel), typeof(TContext)));
    //        }
    //        else
    //            repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType);


    //        return repo;
    //    }

    //    public override IDBViewContext CreateContext()
    //    {
    //        return new TContext();
    //    }

    //    [Obsolete("Create new Business Object to insure all business rules are applied")]
    //    public static IQueryable<TViewModel> QuickGet(Object dynamicFilters = null)
    //    {
    //        var repository = new TContext();
    //        var source = repository.GetIPersistenceSet<TModel>();

    //        IQueryable<TViewModel> viewModels;

    //        viewModels = source.Map<TModel, TViewModel>(dynamicFilters);
    //        return viewModels;
    //    }

    //    [Obsolete("Create new Business Object to insure all business rules are applied")]
    //    public static TViewModel QuickGet(params object[] ids)
    //    {
    //        try
    //        {
    //            var repository = new TContext();
    //            var source = repository.GetIPersistenceSet<TModel>();

    //            TViewModel viewModel;
    //            viewModel = source.Find(RepoExtentions.GetTypedIDs<TViewModel>(ids)).Map<TModel, TViewModel>();

    //            return viewModel;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new Exception(String.Format("Error Getting {0} for ID: {1}", typeof(TModel).Name, ids.ToCommaDeleminatedList()), ex);
    //        }
    //    }
    //}

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
        public OperationType OperationType { get; private set; }

        public ViewModelEventArgs(TViewModel viewModel, OperationType operationType)
        {
            ViewModel = viewModel;
            OperationType = operationType;
        }
    }

    public class ViewModelListEventArgs : EventArgs
    {
        public IQueryable ViewModels { get; private set; }

        public ViewModelListEventArgs(IQueryable viewModels)
        {
            ViewModels = viewModels;
        }
    }

    public class ViewModelEventArgs : EventArgs
    {
        public Object ViewModel { get; private set; }

        public ViewModelEventArgs(Object viewModel)
        {
            ViewModel = viewModel;
        }
    }

    public class MapDelegateArgs<TModel, TViewModel>
    {
        public TViewModel ViewModel { get; private set; }
        public TModel Model { get; private set; }

        public MapDelegateArgs(TModel model, TViewModel viewModel)
        {
            Model = model;
            ViewModel = viewModel;
        }
    }

    public class SaveDelegateArgs<TModel, TViewModel>
    {
        public TViewModel ViewModel { get; private set; }
        public TModel PrestineModel { get; private set; }
        public TModel Model { get; private set; }
        public Result<TViewModel> Result { get; private set; }

        public SaveDelegateArgs(TModel model, TModel prestineModel, TViewModel viewModel, Result<TViewModel> result)
        {
            Model = model;
            PrestineModel = prestineModel;
            ViewModel = viewModel;
            Result = result;
        }
    }

    public class AfterGetDelegateArgs<TViewModel>
    {
        public TViewModel ViewModel { get; private set; }

        public AfterGetDelegateArgs(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }

    public class BeforeGetDelegateArgs
    {

    }

    public class GetListDelegateArgs<TViewModel>
    {
        public IQueryable<TViewModel> ViewModels { get; private set; }

        public GetListDelegateArgs(IQueryable<TViewModel> viewModels)
        {
            ViewModels = viewModels;
        }
    }

    public class CachedResult<TModel, TViewModel>
    {
        public TViewModel ViewModel { get; private set; }
        public TModel Model { get; private set; }

        public CachedResult(TModel model, TViewModel viewModel)
        {
            Model = model;
            ViewModel = viewModel;
        }

        public void Update(TModel model, TViewModel viewModel)
        {
            Model = model;
            ViewModel = viewModel;
        }
    }

    public class GetModelArgs<TModel, TViewModel>
        where TModel : class
        where TViewModel : class, new()
    {
        public Object[] Ids { get; private set; }
        public Object Filters { get; set; }
        [JsonIgnore]
        public Repository<TModel, TViewModel> Repository { get; set; }

        public GetModelArgs(Object[] ids, Object filters, Repository<TModel, TViewModel> repository)
        {
            Ids = ids;
            Filters = filters;
            Repository = repository;
        }

    }
}