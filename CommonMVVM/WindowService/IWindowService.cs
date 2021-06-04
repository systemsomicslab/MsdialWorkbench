using System;

namespace CompMs.CommonMVVM.WindowService
{
    public interface IWindowService<TViewModel>
    {
        bool? ShowDialog(TViewModel viewmodel);
        void Show(TViewModel viewmodel);
    }
}
