namespace BBRAPIModules
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireModuleAttribute : Attribute
    {
        public Type ModuleType { get; }

        public RequireModuleAttribute(Type moduleType)
        {
            this.ModuleType = moduleType;
        }
    }
}
