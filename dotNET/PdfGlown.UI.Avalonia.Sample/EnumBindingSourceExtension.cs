using System;

namespace PdfGlown.UI.Aval.Sample
{
    public class EnumBindingSourceExtension<T>
    {
        public static readonly EnumBindingSourceExtension<T> Instance = new();
        public Type Type => typeof(T);

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type is null || !Type.IsEnum)
                throw new Exception("You must provide a valid enum type");

            return Enum.GetValues(Type);
        }
    }
}
