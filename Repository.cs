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

namespace Joe.Business
{

    public abstract class Repository : IRepository
    {
        public abstract void SetCrud(Object viewModel, Boolean listMode = false);
        public abstract void MapRepoFunction(Object viewModel, Boolean getModel = true);
        public abstract IEnumerable Get(String filter = null);
        public abstract IDBViewContext CreateContext();
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
        protected internal static Object CreateObject(Type type)
        {

            var newExpression = Expression.New(type);
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return lambdaExpression.Compile().DynamicInvoke();
        }
    }

    public abstract class Repository<TModel, TViewModel, TContext> : Repository, IRepository<TModel, TViewModel, TContext>
        where TModel : class
        where TViewModel : class, new()
        where TContext : IDBViewContext, new()
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
        protected INotificationProvider NotificationProvider { get; set; }
        protected IEmailProvider EmailProvider { get; set; }
        private IDBViewContext _repository;
        protected IDBViewContext Context
        {
            get
            {
                _repository = _repository ?? this.CreateContext();
                return _repository;
            }
            set
            {
                _repository = value;
            }
        }
        protected IEnumerable<String> GetIncludeMappings { get; private set; }
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
        protected static Dictionary<String, Object> FilterDictionary = new Dictionary<string, object>();

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

        public Repository(ISecurity<TModel> security, IDBViewContext repositiory)
        {
            Configuration = (BusinessConfigurationAttribute)GetType().GetCustomAttributes(typeof(BusinessConfigurationAttribute), true).SingleOrDefault() ?? new BusinessConfigurationAttribute();
            Context = repositiory;
            Security = security ?? this.TryGetSecurityForModel();
            NotificationProvider = Joe.Business.Notification.NotificationProvider.ProviderInstance;
            EmailProvider = EmailProviderIntance.EmailProvider;
            var includeAttribute = typeof(TViewModel).GetCustomAttribute<IncludeAttribute>();
            GetIncludeMappings = includeAttribute != null ? includeAttribute.IncludeMappings : new List<String>();
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

        public virtual TViewModel Create(TViewModel viewModel, Object dynamicFilters = null)
        {
            try
            {
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
                    if (!this.Configuration.EnforceSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                    {
                        if (this.BeforeCreateInitialSave != null)
                            this.BeforeCreateInitialSave(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));
                        Context.SaveChanges();
                        saved = true;
                    }
                    else
                        throw new System.Security.SecurityException("Access to update denied.");
                });

                try
                {
                    if (this.BeforeCreate != null)
                        this.BeforeCreate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));
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
                if (!this.Configuration.EnforceSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                    FlushListCache(typeof(TModel));
                    viewModel = model.Map<TModel, TViewModel>(dynamicFilters);

                    this.LogFilter(dynamicFilters);

                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Create, model, null, this.EmailProvider);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);


                if (this.AfterCreate != null)
                    this.AfterCreate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));
                if (this.ViewModelCreated != null)
                    this.ViewModelCreated(this, new ViewModelEventArgs<TViewModel>(viewModel));

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
            //Stop get by id from being called when just getting a list with no optional parameters
            return Get(filter: null);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null,
            int? take = null,
            int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapRepoFunctionsOverride = true,
            Boolean descending = false,
            Object dyanmicFilter = null,
            params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, setCrudOverride, mapRepoFunctionsOverride, descending, null, dyanmicFilter, false, orderBy);
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
            Boolean setCount = true,
            params String[] orderBy
            )
        {
            Boolean inMemory = false;
            IQueryable<TViewModel> viewModels;
            IQueryable<TViewModel> source;
            if (Configuration.GetListFromCache && dyanmicFilters == null)
            {
                var cachedViewModels = StaticCacheHelper.GetListCache<TModel, TViewModel>();
                if (cachedViewModels == null)
                {
                    Joe.Caching.Cache.Instance.Add(ListCacheKey, new TimeSpan(BusinessConfigurationSection.Instance.CacheDuration, 0, 0), (Func<Object>)delegate()
                {
                    return StaticCacheHelper.AddCacheItem<TContext>(new Tuple<Type, Type>(typeof(TModel), typeof(TViewModel)));
                });

                    cachedViewModels = StaticCacheHelper.GetListCache<TModel, TViewModel>();
                }

                source = CopyList(cachedViewModels).AsQueryable();
            }
            else
                source = this.Source.Map<TModel, TViewModel>(dyanmicFilters);

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
                this.SetCrud(viewModelList, this.ImplementsICrud, true);

                if (this.Configuration.EnforceSecurity)
                {
                    if (take.HasValue)
                    {
                        int runningSkip = skip ?? 0;
                        viewModels = viewModelList.Where(vm => this.Security.CanRead(this.GetModel, vm)).AsQueryable();
                        while (viewModels.Count() < take && runningSkip < count && count > take)
                        {

                            runningSkip = runningSkip + take.Value;
                            if (viewModels.Count() < take && runningSkip < count && count > take.Value)
                            {
                                var paddedViewModels = source;
                                if (orderBy.Count() > 0 && orderBy.First() != null)
                                    if (!descending)
                                        paddedViewModels = paddedViewModels.OrderBy(orderBy);
                                    else
                                        paddedViewModels = paddedViewModels.OrderByDescending(orderBy);
                                var paddedViewModelList = paddedViewModels.Take(take.Value).Skip(runningSkip).ToList();
                                paddedViewModelList = paddedViewModelList.Where(vm => this.Security.CanRead(this.GetModel, vm)).ToList();
                                this.SetCrud(paddedViewModels, this.ImplementsICrud, true);
                                viewModels = viewModels.Union(paddedViewModelList);
                            }

                        }

                        //if (setCount && take.HasValue && count > take.Value)
                        //    count = count - (take.Value - viewModels.Count()); 

                        viewModels = viewModels.Take(take.Value);
                    }
                    else
                    {
                        viewModels = viewModelList.Where(vm => this.Security.CanRead(this.GetModel, vm)).AsQueryable();
                        count = viewModels.Count();
                    }
                }
                else
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
                    if (AfterMap != null)
                        AfterMap(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));
                    if (ViewModelMapped != null)
                        ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                    //this.MapRepoFunction(viewModel);

                    if (this.Configuration.SetCrud && setCrud)
                        this.SetCrud(viewModel, this.ImplementsICrud);

                    if (this.AfterGet != null)
                        this.AfterGet(new AfterGetDelegateArgs<TViewModel>(viewModel));
                    if (!this.Configuration.EnforceSecurity || this.Security.CanRead(this.GetModel, viewModel))
                    {
                        if (this.ViewModelRetrieved != null)
                            this.ViewModelRetrieved(this, new ViewModelEventArgs<TViewModel>(viewModel));
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

        public virtual TViewModel Update(TViewModel viewModel, Object dynamicFilters = null)
        {
            TModel model = null;
            try
            {
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
                    this.BeforeUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));

                if (!this.Configuration.EnforceSecurity || this.Security.CanUpdate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Update, model, prestineModel, this.EmailProvider);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                viewModel = model.Map<TModel, TViewModel>(dynamicFilters);

                FlushSingleItemAndListCache(viewModel, model, dynamicFilters);

                if (AfterMap != null)
                    AfterMap(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                this.MapRepoFunction(viewModel);

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterUpdate != null)
                    this.AfterUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));
                if (this.ViewModelUpdated != null)
                    ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(viewModel));

                return viewModel;
            }
            catch (Exception ex)
            {
                if (model != null)
                    //this.Context.ObjectContext.Refresh(System.Data.Entity.Core.Objects.RefreshMode.StoreWins, model);
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
                //foreach (var viewModel in viewModelList)
                //{

                //}

                List<Tuple<TModel, TModel, TViewModel>> modelPrestineModelViewModelList = new List<Tuple<TModel, TModel, TViewModel>>();
                foreach (var viewModel in viewModelList)
                {
                    var model = this.Source.WhereVM(viewModel);
                    var prestineModel = model.ShallowClone();

                    if (this.BeforeMapBack != null)
                        this.BeforeMapBack(new MapDelegateArgs<TModel, TViewModel>(model, viewModel));

                    model.MapBack(viewModel, this.Context);

                    if (this.BeforeUpdate != null)
                        this.BeforeUpdate(new SaveDelegateArgs<TModel, TViewModel>(model, prestineModel, viewModel));
                    if (this.Configuration.EnforceSecurity && !this.Security.CanUpdate(this.GetModel, viewModel))
                        throw new System.Security.SecurityException("Access to update denied.");

                    modelPrestineModelViewModelList.Add(new Tuple<TModel, TModel, TViewModel>(model, prestineModel, viewModel));
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

                    FlushSingleItemAndListCache(modelTuple.Item3, modelTuple.Item1, dynamicFilters);
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(new SaveDelegateArgs<TModel, TViewModel>(modelTuple.Item1, modelTuple.Item2, modelTuple.Item3));
                    if (this.ViewModelUpdated != null)
                        this.ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(modelTuple.Item3));
                }

                var modelList = modelPrestineModelViewModelList.Select(modelTuple => modelTuple.Item1).AsQueryable();
                var returnList = modelList.Map<TModel, TViewModel>(dynamicFilters);

                returnList.ForEach(vm =>
                {
                    if (AfterMap != null)
                        AfterMap(new MapDelegateArgs<TModel, TViewModel>(modelList.WhereVM(vm), vm));
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
                    this.BeforeDelete(new SaveDelegateArgs<TModel, TViewModel>(model, model, viewModel));

                if (!this.Configuration.EnforceSecurity || this.Security.CanDelete(this.GetModel, viewModel))
                {
                    this.Source.Remove(model);
                    this.Context.SaveChanges();
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Delete, model, null, this.EmailProvider);

                    var getModelArgs = new GetModelArgs(viewModel.GetIDs().ToArray(), null, this.Context);
                    this.FlushSingleItemAndListCache(viewModel, model, null);
                }
                else
                    throw new System.Security.SecurityException("Access to delete denied.");
                if (this.AfterDelete != null)
                    this.AfterDelete(new SaveDelegateArgs<TModel, TViewModel>(model, model, viewModel));
                if (this.ViewModelDeleted != null)
                    this.ViewModelDeleted(this, new ViewModelEventArgs<TViewModel>(viewModel));
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
                                    var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelType, this.Context.GetType());
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
                                var nestedViewRepo = Repository.CreateRepo(nestRepoMap.Repository, nestRepoMap.Model, viewModelInfo.PropertyType, this.Context.GetType());
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
                                var allValuesList = allValuesMap.Repository != null ?
                                    Repository.CreateRepo(allValuesMap.Repository, allValuesMap.Model, viewModelType, typeof(TContext)).Get(allValuesMap.Filter)
                                    : this.Context.GetIQuery(allValuesMap.Model).Map(viewModelType).Filter(allValuesMap.Filter);
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
                                        || prop.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() != null);

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

        public override IDBViewContext CreateContext()
        {
            return new TContext();
        }

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
            var ids = viewModel.GetIDs().ToArray();
            var getModelArgs = new GetModelArgs(ids, dynamicFilters, this.Context);
            var cacheResult = (CachedResult<TModel, TViewModel>)Caching.Cache.Instance.Get(CacheKey, getModelArgs);
            if (cacheResult != null)
                cacheResult.Update(model, viewModel);

            FlushTypeByFullName(typeof(TModel), dynamicFilters, false, ids);

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
                    var getModelArgs = new GetModelArgs(modelIDs, filter, this.Context);
                    Joe.Caching.Cache.Instance.FlushMany(type.FullName, CacheKey, getModelArgs);
                }
                if (flushNullFilter)
                {
                    var getModelArgs = new GetModelArgs(modelIDs, null, this.Context);
                    Joe.Caching.Cache.Instance.FlushMany(type.FullName, null, getModelArgs);
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

        private CachedResult<TModel, TViewModel> GetCachedResultDelegate(GetModelArgs args)
        {
            //var context = this.CreateContext();
            TModel model;
            if (this.GetIncludeMappings.Count() > 0)
                model = args.Context.GetIDbSet<TModel>().BuildIncludeMappings(this.GetIncludeMappings.ToArray()).Find<TModel, TViewModel>(true, this.GetTypedIDs(args.Ids));
            else
                model = args.Context.GetIDbSet<TModel>().Find(this.GetTypedIDs(args.Ids));

            if (model != null)
            {
                //The First BO will be long lived. Since the context is disposed we must reset it;
                this.Context = args.Context;
                var viewModel = model.Map<TModel, TViewModel>(args.Filters);
                this.LogFilter(args.Filters);
                this.MapRepoFunction(viewModel);
                args.Context.Detach(model);
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
            var modelIDs = new GetModelArgs(this.GetTypedIDs(ids), dyanmicFilters, context);
            cachedResults = (CachedResult<TModel, TViewModel>)Joe.Caching.Cache.Instance.Get(CacheKey, modelIDs);

            if (cachedResults == null)
            {
                Delegate getCachedModel = (Func<GetModelArgs, CachedResult<TModel, TViewModel>>)((GetModelArgs args) =>
                {
                    return GetCachedResultDelegate(args);
                });
                Joe.Caching.Cache.Instance.Add(CacheKey, new TimeSpan(cacheTimeout, 0, 0), getCachedModel);
                cachedResults = (CachedResult<TModel, TViewModel>)Joe.Caching.Cache.Instance.Get(CacheKey, modelIDs);
            }
            return cachedResults;
        }

        private void LogFilter(Object filter)
        {
            if (filter != null)
            {
                var key = this.GetFilterKey(filter);

                if (!FilterDictionary.ContainsKey(key))
                    FilterDictionary.Add(key, filter);
            }
        }

        private String GetFilterKey(Object filter)
        {
            return typeof(TModel).FullName + filter.GetHashCode();
        }

        #endregion

        public void Dispose()
        {
            this.Context.Dispose();
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

        public SaveDelegateArgs(TModel model, TModel prestineModel, TViewModel viewModel)
        {
            Model = model;
            PrestineModel = prestineModel;
            ViewModel = viewModel;
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

    public class GetModelArgs
    {
        public Object[] Ids { get; private set; }
        public Object Filters { get; set; }
        [JsonIgnore]
        public IDBViewContext Context { get; set; }

        public GetModelArgs(Object[] ids, Object filters, IDBViewContext context)
        {
            Ids = ids;
            Filters = filters;
            Context = context;
        }

    }
}