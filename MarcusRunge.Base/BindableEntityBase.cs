namespace MarcusRunge.Base
{
    /// <summary>
    /// Provides a base class for bindable entities with database identity
    /// and optimistic concurrency support.
    /// </summary>
    public abstract class BindableEntityBase : BindableBase
    {
        private int _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindableEntityBase"/> class.
        /// </summary>
        protected BindableEntityBase()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindableEntityBase"/> class
        /// with the specified database identity.
        /// </summary>
        /// <param name="id">The database identity.</param>
        protected BindableEntityBase(int id) => _id = id;

        /// <summary>
        /// Gets or sets the database identity.
        /// </summary>
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the concurrency token used for optimistic concurrency control.
        /// </summary>
        /// <remarks>
        /// Changes to this persistence-managed concurrency token do not raise property change notifications.
        /// </remarks>
        public byte[]? RowVersion { get; set; }
    }
}