using Joe.Business.Notification;
using Joe.MapBack;
using Joe.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace Joe.Business
{
    public abstract class Repository<TModel> : Repository
      where TModel : class, new()
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
        protected new IEmailProvider EmailProvider { get; set; }
        private IDBViewContext _repository;
        protected IDBViewContext Context
        {
            get
            {
                return _repository;
            }
            set
            {
                _repository = value;
            }
        }

        #region Delegates
        protected delegate void MapDelegate(TModel model, Object viewMode);
        protected delegate void SaveDelegate(TModel model, Object viewModel);
        protected delegate void AfterGetDelegate(Object viewModel);
        protected delegate void BeforeGetDelegate(IDBViewContext repository);
        protected delegate IQueryable GetListDelegate(IQueryable viewModels);
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
        public delegate IQueryable ViewModelListEvent(Object sender, ViewModelListEventArgs viewModelListEventArgs);
        public delegate void ViewModelEvent(Object sender, ViewModelEventArgs viewModelEventArgs);
        public event ViewModelListEvent ViewModelListRetrieved;
        public event ViewModelEvent ViewModelCreated;
        public event ViewModelEvent ViewModelUpdated;
        public event ViewModelEvent ViewModelRetrieved;
        public event ViewModelEvent ViewModelDeleted;
        public event ViewModelEvent ViewModelMapped;
        public event ViewModelListEvent ViewModelListMapped;
        #endregion

        //public new static IRepository<TModel, TViewModel, TContext> CreateRepo(Type RepositoryType)
        //{
        //    IRepository<TModel, TViewModel, TContext> repo;
        //    if (RepositoryType.GetGenericArguments().Count() == 2)
        //        repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TViewModel), typeof(TContext)));
        //    else if (RepositoryType.GetGenericArguments().Count() == 1)
        //        repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TContext)));
        //    else
        //        repo = (IRepository<TModel, TViewModel, TContext>)CreateObject(RepositoryType.MakeGenericType(typeof(TModel), typeof(TViewModel), typeof(TContext)));

        //    return repo;
        //}

        public Repository(ISecurity<TModel> security, IDBViewContext repositiory)
        {
            Configuration = (BusinessConfigurationAttribute)GetType().GetCustomAttributes(typeof(BusinessConfigurationAttribute), true).SingleOrDefault() ?? new BusinessConfigurationAttribute();
            Context = repositiory;
            Security = security ?? this.TryGetSecurityForModel();
            NotificationProvider = Joe.Business.Notification.NotificationProvider.ProviderInstance;
            EmailProvider = Repository.EmailProvider;
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

        public virtual TViewModel Create<TViewModel>(TViewModel viewModel, Object dynamicFilters = null)
            where TViewModel : class, new()
        {
            try
            {
                var model = Source.Create();

                if (Configuration.IncrementKey)
                    SetNewKey(viewModel);


                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(model, viewModel, Context);

                model = this.Source.Add(model);
                model.MapBack(viewModel, this.Context, () =>
                {
                    if (!this.Configuration.UseSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                    {
                        Context.SaveChanges();
                    }
                    else
                        throw new System.Security.SecurityException("Access to update denied.");
                });

                if (this.BeforeCreate != null)
                    this.BeforeCreate(model, viewModel, Context);

                if (!this.Configuration.UseSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                    viewModel = model.Map<TModel, TViewModel>(dynamicFilters);
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Create, model, null, this.EmailProvider);
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

        public virtual IQueryable<TViewModel> Get<TViewModel>()
            where TViewModel : class, new()
        {
            return Get<TViewModel>((Expression<Func<TViewModel, Boolean>>)null);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(filter, null, null, Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter, Boolean setCrudOverride)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(filter, null, null, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(int? take, int? skip)
            where TViewModel : class, new()
        {
            return Get<TViewModel>((Expression<Func<TViewModel, Boolean>>)null, take, skip, this.Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(int? take, int? skip, Boolean setCrudOverride)
            where TViewModel : class, new()
        {
            return Get<TViewModel>((Expression<Func<TViewModel, Boolean>>)null, take, skip, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean setCrudOverride)
            where TViewModel : class, new()
        {
            int count;
            return Get<TViewModel>(out count, filter, take, skip, false, setCrudOverride);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(filter, take, skip, this.Configuration.SetCrud);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy)
            where TViewModel : class, new()
        {
            int count;
            return Get<TViewModel>(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
            where TViewModel : class, new()
        {
            int count;
            return Get<TViewModel>(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get<TViewModel>(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapRepositoryFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get<TViewModel>(Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapRepoFunctionsOverride = true, Boolean descending = false, Object dyanmicFilter = null, params String[] orderBy)
            where TViewModel : class, new()
        {
            int count;
            return Get<TViewModel>(out count, filter, take, skip, setCrudOverride, mapRepoFunctionsOverride, descending, null, dyanmicFilter, orderBy);
        }

        /// <summary>
        /// Get a list of ViewModels
        /// </summary>
        /// <returns>Returns a list of ViewModesl. If UseSecurity and SetCrud are set to true then the list will be filtered to only return results the user has access too.</returns>
        public virtual IQueryable<TViewModel> Get<TViewModel>(out int count,
            Expression<Func<TViewModel, Boolean>> filter = null,
            int? take = null, int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapRepoFunctionsOverride = true,
            Boolean descending = false,
            String stringfilter = null,
            Object dyanmicFilters = null,
            params String[] orderBy
            )
            where TViewModel : class, new()
        {
            Boolean inMemory = false;
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
                inMemory = true;
                this.SetCrud(viewModelList, this.ImplementsICrud, true);


                if (this.Configuration.UseSecurity)
                {
                    viewModels = viewModelList.Where(vm => this.Security.CanRead(this.GetModel, vm)).AsQueryable();
                    count = viewModels.Count();
                }
                else
                    viewModels = viewModelList.AsQueryable();
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
                viewModels = ViewModelListMapped(this, new ViewModelListEventArgs(viewModels));

            if (this.Configuration.MapRepositoryFunctionsForList && mapRepoFunctionsOverride)
            {
                if (!inMemory)
                    //Load the list into memory so any changes are saved to the List returned to calling function
                    viewModels = viewModels.ToList().AsQueryable();
                viewModels.ForEach(vm => this.MapRepoFunction(vm));
            }

            if (this.BeforeReturnList != null)
                viewModels = this.BeforeReturnList(viewModels, this.Context);
            if (this.ViewModelListRetrieved != null)
                viewModels = ViewModelListRetrieved(this, new ViewModelListEventArgs(viewModels));

            return viewModels;
        }

        public IEnumerable Get<TViewModel>(string filter = null)
        {
            int count = 0;
            return this.Get<TViewModel>(out count, stringfilter: filter, setCrudOverride: false, mapRepoFunctionsOverride: false);
        }

        #endregion

        public virtual TViewModel Get<TViewModel>(params Object[] ids)
            where TViewModel : class, new()
        {
            return Get<TViewModel>(null, true, ids);
        }

        public virtual TViewModel GetWithFilters<TViewModel>(Object dynamicFilters, params Object[] ids)
            where TViewModel : class, new()
        {
            return Get(dynamicFilters, true, ids);
        }

        public virtual TViewModel Get<TViewModel>(Object dyanmicFilters, Boolean setCrud, params Object[] ids)
            where TViewModel : class, new()
        {
            try
            {
                TViewModel viewModel;

                if (this.BeforeGet != null)
                    this.BeforeGet(this.Context);
                var model = this.Source.Find(this.GetTypedIDs(ids));
                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);
                if (AfterMap != null)
                    AfterMap(model, viewModel);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs(viewModel));

                this.MapRepoFunction(viewModel);

                if (this.Configuration.SetCrud && setCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterGet != null)
                    this.AfterGet(viewModel);
                if (!this.Configuration.UseSecurity || this.Security.CanRead(this.GetModel, viewModel))
                {
                    if (this.ViewModelRetrieved != null)
                        this.ViewModelRetrieved(this, new ViewModelEventArgs(viewModel));
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Read, model, null, this.EmailProvider);
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

        public virtual TViewModel Update<TViewModel>(TViewModel viewModel, Object dyanmicFilters = null)
            where TViewModel : class, new()
        {
            try
            {
                TModel model = this.Source.WhereVM(viewModel);
                var prestineModel = this.Source.AsNoTracking().WhereVM(viewModel);
                if (this.BeforeMapBack != null)
                    this.BeforeMapBack(model, viewModel);

                model.MapBack(viewModel, this.Context);

                if (this.BeforeUpdate != null)
                    this.BeforeUpdate(model, viewModel);

                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);

                if (AfterMap != null)
                    AfterMap(model, viewModel);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs(viewModel));

                this.MapRepoFunction(viewModel);

                if (!this.Configuration.UseSecurity || this.Security.CanUpdate(this.GetModel, viewModel))
                {
                    this.Context.SaveChanges();
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Update, model, prestineModel, this.EmailProvider);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterUpdate != null)
                    this.AfterUpdate(model, viewModel);
                if (this.ViewModelUpdated != null)
                    ViewModelUpdated(this, new ViewModelEventArgs(viewModel));

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

        public virtual IQueryable<TViewModel> Update<TViewModel>(List<TViewModel> viewModelList, Object dynamicFilters = null)
            where TViewModel : class, new()
        {
            try
            {
                foreach (var viewModel in viewModelList)
                {
                    if (this.BeforeMapBack != null)
                        this.BeforeMapBack(this.Source.WhereVM(viewModel), viewModel);
                }

                List<Tuple<TModel, TModel, TViewModel>> modelPrestineModelViewModelList = new List<Tuple<TModel, TModel, TViewModel>>();
                foreach (var viewModel in viewModelList)
                {
                    var model = this.Source.WhereVM(viewModel);
                    var prestineModel = this.Source.AsNoTracking().WhereVM(viewModel);

                    model.MapBack(viewModel, this.Context);

                    if (this.BeforeUpdate != null)
                        this.BeforeUpdate(model, viewModel);
                    if (this.Configuration.UseSecurity && !this.Security.CanUpdate(this.GetModel, viewModel))
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
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(modelTuple.Item1, modelTuple.Item3);
                    if (this.ViewModelUpdated != null)
                        this.ViewModelUpdated(this, new ViewModelEventArgs(modelTuple.Item3));
                }

                FlushViewModelCache();
                var modelList = modelPrestineModelViewModelList.Select(modelTuple => modelTuple.Item1).AsQueryable();
                var returnList = modelList.Map<TModel, TViewModel>(dynamicFilters);

                returnList.ForEach(vm =>
                {
                    if (AfterMap != null)
                        AfterMap(modelList.WhereVM(vm), vm);
                    if (ViewModelMapped != null)
                        ViewModelMapped(this, new ViewModelEventArgs(vm));

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

        public virtual void Delete<TViewModel>(params Object[] ids)
            where TViewModel : class, new()
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

        public virtual void Delete<TViewModel>(TViewModel viewModel)
            where TViewModel : class, new()
        {
            try
            {

                TModel model = Source.WhereVM(viewModel);
                if (this.BeforeDelete != null)
                    this.BeforeDelete(model, viewModel);

                if (!this.Configuration.UseSecurity || this.Security.CanDelete(this.GetModel, viewModel))
                {
                    this.Source.Remove(model);
                    this.Context.SaveChanges();
                    if (this.NotificationProvider != null)
                        this.NotificationProvider.ProcessNotifications(typeof(TModel).FullName, NotificationType.Delete, model, null, this.EmailProvider);
                }
                else
                    throw new System.Security.SecurityException("Access to delete denied.");
                if (this.AfterDelete != null)
                    this.AfterDelete(model, viewModel);
                if (this.ViewModelDeleted != null)
                    this.ViewModelDeleted(this, new ViewModelEventArgs(viewModel));
                FlushViewModelCache();
            }
            catch (Exception ex)
            {
                if (ex is ValidationException)
                    throw ex;

                throw new Exception("Error Deleting: " + typeof(TModel).Name, ex);
            }
        }

        public Boolean Exists<TViewModel>(TViewModel viewModel)
            where TViewModel : class, new()
        {
            return this.Source.WhereVM(viewModel) != null;
        }

        public Boolean Exists<TViewModel>(params Object[] ids)
        {
            return this.Source.Find(this.GetTypedIDs(ids)) != null;
        }

        public virtual TViewModel Default<TViewModel>(TViewModel defaultValues = null)
            where TViewModel : class, new()
        {
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
                this.Context.ObjectContext.Detach(model);
            }
            else
                viewModel = model.Map<TModel, TViewModel>();

            return viewModel;
        }

        public void MapRepoFunction(Object viewModel, Boolean getModel = true)
        {
            foreach (PropertyInfo viewModelInfo in viewModel.GetType().GetProperties())
            {
                try
                {
                    var repoMap = viewModelInfo.GetCustomAttributes(typeof(RepoMappingAttribute), true).SingleOrDefault() as RepoMappingAttribute;
                    var nestRepoMap = viewModelInfo.GetCustomAttributes(typeof(NestedRepoMappingAttribute), true).SingleOrDefault() as NestedRepoMappingAttribute;
                    var allValuesMap = viewModelInfo.GetCustomAttributes(typeof(AllValuesAttribute), true).SingleOrDefault() as AllValuesAttribute;
                    var dyanmicFilterss = viewModelInfo.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() as DynamicFilterAttribute;

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
                            var allValuesList = allValuesMap.Repository != null ?
                                Repository.CreateRepo(allValuesMap.Repository, allValuesMap.Model, viewModelType, typeof(TContext)).Get(allValuesMap.Filter)
                                : this.Context.GetIQuery(allValuesMap.Model).Map(viewModelType).Filter(allValuesMap.Filter);
                            var list = Repository.CreateObject(typeof(List<>).MakeGenericType(viewModelType));
                            var listAddMethod = list.GetType().GetMethod("Add");
                            if (!String.IsNullOrWhiteSpace(allValuesMap.IncludedList))
                            {
                                var includedValues = ReflectionHelper.GetEvalProperty(viewModel, allValuesMap.IncludedList) as IEnumerable;
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
                            viewModelInfo.SetValue(viewModel, allValuesList);
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
        protected virtual object SetNewKey<TViewModel>(TViewModel viewModel)
        {
            int id = this.Source.NewKey<TModel, TViewModel>();
            viewModel.SetIDs(id);
            return id;
        }

        protected virtual void FlushViewModelCache<TViewModel>()
            where TViewModel : class, new()
        {
            StaticCacheHelper.Flush(typeof(TViewModel).Name.Replace("View", "ListView"));
            StaticCacheHelper.Flush(typeof(TViewModel).Name);
        }

        protected internal TModel GetModel<TViewModel>(TViewModel viewModel)
        {
            return this.Source.WhereVM(viewModel);
        }

        protected Boolean ImplementsICrud<TViewModel>()
        {

            return typeof(TViewModel).GetInterface(typeof(ICrud).Name) != null;
        }

        public void SetCrud<TViewModel>(IEnumerable<TViewModel> viewModelList, Boolean iCrud, Boolean listMode = false)
        {
            foreach (var viewModel in viewModelList)
            {
                SetCrud(viewModel, iCrud, listMode);
            }
        }

        public void SetCrud<TViewModel>(TViewModel viewModel, Boolean iCrud, Boolean listMode = false)
        {
            if (iCrud)
                Security.SetCrud(this.GetModel, viewModel, listMode);
            else
                Security.SetCrudReflection(this.GetModel, viewModel, listMode);
        }

    }
}
