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

    public abstract class BusinessObject : IBusinessObject
    {
        public abstract void SetCrud(Object viewModel, Boolean listMode = false);
        public abstract void MapBOFunction(Object viewModel, Boolean getModel = true);
        public abstract IEnumerable Get(String filter = null);
        public static IEmailProvider EmailProvider { get; set; }
        public static ISecurityFactory _securityFactory;
        public static ISecurityFactory BOSecurityFactory
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
        /// <param name="businessObjectType">Must Already Contain generic Parameters</param>
        /// <returns></returns>
        public static IBusinessObject CreateBO(Type businessObjectType)
        {
            var boInterface = businessObjectType.GetInterface("IBusinessObject`3");
            var model = boInterface.GetGenericArguments().First();
            var viewModel = boInterface.GetGenericArguments().ToArray()[1];
            var repository = boInterface.GetGenericArguments().ToArray()[2];
            var genericBO = typeof(BusinessObject<,,>).MakeGenericType(model, viewModel, repository);
            var method = genericBO.GetMethod("CreateBO");
            var bo = (IBusinessObject)method.Invoke(null, new Object[] { businessObjectType });

            return bo;
        }
        public static IBusinessObject CreateBO(Type businessObjectType, Type model, Type viewModel, Type repository)
        {
            var genericBO = typeof(BusinessObject<,,>).MakeGenericType(model, viewModel, repository);
            var method = genericBO.GetMethod("CreateBO");
            var bo = (IBusinessObject)method.Invoke(null, new Object[] { businessObjectType });

            return bo;
        }
        protected static Object CreateObject(Type type)
        {

            var newExpression = Expression.New(type);
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return lambdaExpression.Compile().DynamicInvoke();
        }
    }


    public abstract class BusinessObject<TModel, TViewModel, TRepository> : BusinessObject, IBusinessObject<TModel, TViewModel, TRepository>
        where TModel : class, new()
        where TViewModel : class, new()
        where TRepository : class, IDBViewContext, new()
    {
        private IDbSet<TModel> _source;
        protected virtual IDbSet<TModel> Source
        {
            get
            {
                return _source ?? this.Repository.GetIDbSet<TModel>();
            }
            set { _source = value; }
        }
        protected BusinessConfigurationAttribute Configuration { get; set; }
        protected virtual ISecurity<TModel> Security { get; set; }
        private TRepository _repository;
        protected TRepository Repository
        {
            get
            {
                _repository = _repository ?? new TRepository();
                return _repository;
            }
            set
            {
                _repository = value;
            }
        }

        #region Delegates
        protected delegate void MapDelegate(TModel model, TViewModel viewModel, TRepository repository);
        protected delegate void SaveDelegate(TModel model, TViewModel viewModel, TRepository repository);
        protected delegate void AfterGetDelegate(TViewModel viewModel, TRepository repository);
        protected delegate void BeforeGetDelegate(TRepository repository);
        protected delegate IQueryable<TViewModel> GetListDelegate(IQueryable<TViewModel> viewModels, TRepository repository);
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

        public new static IBusinessObject<TModel, TViewModel, TRepository> CreateBO(Type businessObjectType)
        {
            IBusinessObject<TModel, TViewModel, TRepository> bo;
            if (businessObjectType.GetGenericArguments().Count() == 2)
                bo = (IBusinessObject<TModel, TViewModel, TRepository>)CreateObject(businessObjectType.MakeGenericType(typeof(TViewModel), typeof(TRepository)));
            else if (businessObjectType.GetGenericArguments().Count() == 1)
                bo = (IBusinessObject<TModel, TViewModel, TRepository>)CreateObject(businessObjectType.MakeGenericType(typeof(TRepository)));
            else
                bo = (IBusinessObject<TModel, TViewModel, TRepository>)CreateObject(businessObjectType.MakeGenericType(typeof(TModel), typeof(TViewModel), typeof(TRepository)));

            return bo;
        }

        public BusinessObject(ISecurity<TModel> security, TRepository repositiory)
        {
            Configuration = (BusinessConfigurationAttribute)GetType().GetCustomAttributes(typeof(BusinessConfigurationAttribute), true).SingleOrDefault() ?? new BusinessConfigurationAttribute();
            Repository = repositiory;
            Security = security ?? this.TryGetSecurityForModel();
        }

        public BusinessObject(ISecurity<TModel> security)
            : this(security, null)
        {

        }

        public BusinessObject(TRepository repositiory)
            : this(new Security<TModel>(), repositiory)
        {

        }

        public BusinessObject() :
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

            var security = BOSecurityFactory.Create<TModel>();
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
                    this.BeforeMapBack(model, viewModel, Repository);

                model.MapBack(viewModel, this.Repository);
                model = this.Source.Add(model);

                if (this.BeforeCreate != null)
                    this.BeforeCreate(model, viewModel, Repository);

                if (!this.Configuration.UseSecurity || this.Security.CanCreate(this.GetModel, viewModel))
                {
                    this.Repository.SaveChanges();
                    viewModel = model.Map<TModel, TViewModel>(dynamicFilters);
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);


                if (this.AfterCreate != null)
                    this.AfterCreate(model, viewModel, Repository);
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
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapBusinessFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapBusinessFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, params String[] orderBy)
        {
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapBusinessFunctionsForList, false, null, orderBy);
        }

        public IQueryable<TViewModel> Get(out int count, Expression<Func<TViewModel, Boolean>> filter, int? take, int? skip, Boolean descending, params String[] orderBy)
        {
            return Get(out count, filter, take, skip, this.Configuration.SetCrud, this.Configuration.MapBusinessFunctionsForList, descending, null, orderBy);
        }

        public virtual IQueryable<TViewModel> Get(Expression<Func<TViewModel, Boolean>> filter = null, int? take = null, int? skip = null, Boolean setCrudOverride = true, Boolean mapBOFunctionsOverride = true, Boolean descending = false, Object dyanmicFilter = null, params String[] orderBy)
        {
            int count;
            return Get(out count, filter, take, skip, setCrudOverride, mapBOFunctionsOverride, descending, null, dyanmicFilter, orderBy);
        }

        /// <summary>
        /// Get a list of ViewModels
        /// </summary>
        /// <returns>Returns a list of ViewModesl. If UseSecurity and SetCrud are set to true then the list will be filtered to only return results the user has access too.</returns>
        public virtual IQueryable<TViewModel> Get(out int count,
            Expression<Func<TViewModel, Boolean>> filter = null,
            int? take = null, int? skip = null,
            Boolean setCrudOverride = true,
            Boolean mapBOFunctionsOverride = true,
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
                if (!Configuration.GetListFromCache)
                    viewModels = viewModels.ToList().AsQueryable();

                this.SetCrud(viewModels, this.ImplementsICrud, true);

                if (this.Configuration.UseSecurity)
                {
                    viewModels = viewModels.Where(vm => (Boolean)typeof(TViewModel).GetProperty("CanRead").GetValue(vm, null));
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

            if (this.Configuration.MapBusinessFunctionsForList && mapBOFunctionsOverride)
                viewModels.ForEach(vm => this.MapBOFunction(vm));

            if (this.BeforeReturnList != null)
                viewModels = this.BeforeReturnList(viewModels, this.Repository);
            if (this.ViewModelListRetrieved != null)
                viewModels = ViewModelListRetrieved(this, new ViewModelListEventArgs<TViewModel>(viewModels));

            return viewModels;
        }

        public override IEnumerable Get(string filter = null)
        {
            int count = 0;
            return this.Get(out count, stringfilter: filter, setCrudOverride: false, mapBOFunctionsOverride: false);
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
                    this.BeforeGet(this.Repository);
                var model = this.Source.Find(this.GetTypedIDs(ids));
                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);
                if (AfterMap != null)
                    AfterMap(model, viewModel, Repository);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                this.MapBOFunction(viewModel);

                if (this.Configuration.SetCrud && setCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterGet != null)
                    this.AfterGet(viewModel, this.Repository);
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
                    this.BeforeMapBack(model, viewModel, Repository);

                model.MapBack(viewModel, this.Repository);

                if (this.BeforeUpdate != null)
                    this.BeforeUpdate(model, viewModel, Repository);

                viewModel = model.Map<TModel, TViewModel>(dyanmicFilters);

                if (AfterMap != null)
                    AfterMap(model, viewModel, Repository);
                if (ViewModelMapped != null)
                    ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(viewModel));

                this.MapBOFunction(viewModel);

                if (!this.Configuration.UseSecurity || this.Security.CanUpdate(this.GetModel, viewModel))
                {
                    this.Repository.SaveChanges();
                }
                else
                    throw new System.Security.SecurityException("Access to update denied.");

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModel, this.ImplementsICrud);

                if (this.AfterUpdate != null)
                    this.AfterUpdate(model, viewModel, Repository);
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
                        this.BeforeMapBack(this.Source.WhereVM(viewModel), viewModel, Repository);
                }

                var modelList = this.Source.MapBack(viewModelList, this.Repository).AsQueryable();

                foreach (var viewModel in viewModelList)
                {
                    var model = modelList.WhereVM(viewModel);
                    if (this.BeforeUpdate != null)
                        this.BeforeUpdate(model, viewModel, Repository);
                    if (this.Configuration.UseSecurity && !this.Security.CanUpdate(this.GetModel, viewModel))
                        throw new System.Security.SecurityException("Access to update denied.");
                }
                this.Repository.SaveChanges();

                if (this.Configuration.SetCrud)
                    this.SetCrud(viewModelList, this.ImplementsICrud);

                foreach (var viewModel in viewModelList)
                {
                    if (this.AfterUpdate != null)
                        this.AfterUpdate(modelList.WhereVM(viewModel), viewModel, Repository);
                    if (this.ViewModelUpdated != null)
                        this.ViewModelUpdated(this, new ViewModelEventArgs<TViewModel>(viewModel));
                }

                FlushViewModelCache();
                var returnList = modelList.Map<TModel, TViewModel>(dynamicFilters);

                returnList.ForEach(vm =>
                {
                    if (AfterMap != null)
                        AfterMap(modelList.WhereVM(vm), vm, Repository);
                    if (ViewModelMapped != null)
                        ViewModelMapped(this, new ViewModelEventArgs<TViewModel>(vm));

                    this.MapBOFunction(vm);
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
                    this.BeforeDelete(model, viewModel, Repository);
                this.Source.Remove(model);
                if (!this.Configuration.UseSecurity || this.Security.CanDelete(this.GetModel, viewModel))
                    this.Repository.SaveChanges();
                else
                    throw new System.Security.SecurityException("Access to update denied.");
                if (this.AfterDelete != null)
                    this.AfterDelete(model, viewModel, Repository);
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
            var repository = new TRepository();
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
                var repository = new TRepository();
                var source = repository.GetIDbSet<TModel>();

                TViewModel viewModel;
                viewModel = source.Find(BOExtentions.GetTypedIDs<TModel, TViewModel, TRepository>(ids)).Map<TModel, TViewModel>();

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

        public override void MapBOFunction(Object viewModel, Boolean getModel = true)
        {
            if (typeof(TViewModel).IsAssignableFrom(viewModel.GetType()))
                this.MapBOFunction((TViewModel)viewModel, getModel);
            else
                throw new Exception("The Object passed in must be derived from TViewModel");
        }

        public void MapBOFunction(TViewModel viewModel, Boolean getModel = true)
        {
            foreach (PropertyInfo viewModelInfo in viewModel.GetType().GetProperties())
            {
                try
                {
                    var boMap = viewModelInfo.GetCustomAttributes(typeof(BOMappingAttribute), true).SingleOrDefault() as BOMappingAttribute;
                    var nestBOMap = viewModelInfo.GetCustomAttributes(typeof(NestedBOMappingAttribute), true).SingleOrDefault() as NestedBOMappingAttribute;
                    var allValuesMap = viewModelInfo.GetCustomAttributes(typeof(AllValuesAttribute), true).SingleOrDefault() as AllValuesAttribute;
                    var dyanmicFilterss = viewModelInfo.GetCustomAttributes(typeof(DynamicFilterAttribute), true).SingleOrDefault() as DynamicFilterAttribute;
                    if (boMap != null && boMap.HasMethod)
                    {
                        viewModelInfo.SetValue(viewModel,
                            boMap.GetMethodInfo(this, viewModel, typeof(TModel)).Invoke(this, boMap.GetParameters(viewModel,
                            getModel ? this.GetModel : (Func<TViewModel, TModel>)null).ToArray()), null);
                    }
                    if (allValuesMap != null)
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                            var allValuesBO = BusinessObject.CreateBO(allValuesMap.BusinessObject, allValuesMap.Model, viewModelType, typeof(TRepository));

                            viewModelInfo.SetValue(viewModel, allValuesBO.Get(allValuesMap.Filter));
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
                    if (nestBOMap != null)
                    {
                        if (viewModelInfo.PropertyType.ImplementsIEnumerable())
                        {
                            var viewModelType = viewModelInfo.PropertyType.GetGenericArguments().First();
                            var nestedViews = ((IEnumerable)viewModelInfo.GetValue(viewModel)).Cast<Object>();
                            var nestedViewBO = BusinessObject.CreateBO(nestBOMap.BusinessObject, nestBOMap.Model, viewModelType, typeof(TRepository));
                            var list = BusinessObject.CreateObject(typeof(List<>).MakeGenericType(viewModelType));
                            var listAddMethod = list.GetType().GetMethod("Add");
                            foreach (var nestedView in nestedViews)
                            {
                                nestBOMap.SetParameters(viewModel, nestedView);
                                nestedViewBO.MapBOFunction(nestedView);
                                listAddMethod.Invoke(list, new Object[] { nestedView });
                            }

                            viewModelInfo.SetValue(viewModel, list);
                        }
                        else if (viewModelInfo.PropertyType.IsClass)
                        {
                            var nestedView = viewModelInfo.GetValue(viewModel);
                            var nestedViewBO = BusinessObject.CreateBO(nestBOMap.BusinessObject, nestBOMap.Model, viewModelInfo.PropertyType, typeof(TRepository));
                            nestBOMap.SetParameters(viewModel, nestedView);
                            nestedViewBO.MapBOFunction(nestedView);
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
            this.Repository.Dispose();
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