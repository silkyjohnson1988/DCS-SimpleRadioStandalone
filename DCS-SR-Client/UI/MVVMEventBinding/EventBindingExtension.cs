// Decompiled with JetBrains decompiler
// Type: MvvmEventBinding.EventBindingExtension
// Assembly: MvvmEventBinding, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A915A179-3A04-4459-9F13-2FC54A4D273B
// Assembly location: C:\Users\Ciaran\.nuget\packages\mvvmeventbinding\1.0.0\lib\net45\MvvmEventBinding.dll

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.MVVMEventBinding
{
  public class EventBindingExtension : MarkupExtension
  {
    private readonly string _commandName;

    public EventBindingExtension(string command) => this._commandName = command;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      MethodInfo method = this.GetType().GetMethod("InvokeCommand", BindingFlags.Instance | BindingFlags.NonPublic);
      if (method != (MethodInfo) null && serviceProvider.GetService(typeof (IProvideValueTarget)) is IProvideValueTarget service)
      {
        object targetProperty = service.TargetProperty;
        if ((object) (targetProperty as EventInfo) != null)
        {
          Type eventHandlerType = (targetProperty as EventInfo).EventHandlerType;
          return (object) method.CreateDelegate(eventHandlerType, (object) this);
        }
        if ((object) (targetProperty as MethodInfo) != null)
        {
          ParameterInfo[] parameters = (targetProperty as MethodInfo).GetParameters();
          if (parameters.Length >= 1)
          {
            Type parameterType = parameters[1].ParameterType;
            return (object) method.CreateDelegate(parameterType, (object) this);
          }
        }
      }
      throw new InvalidOperationException("The EventBinding markup extension is valid only in the context of events.");
    }

    private void InvokeCommand(object sender, EventArgs args)
    {
      if (string.IsNullOrEmpty(this._commandName) || !(sender is FrameworkElement frameworkElement))
        return;
      object dataContext = frameworkElement.DataContext;
      if (dataContext == null)
        return;
      PropertyInfo property = dataContext.GetType().GetProperty(this._commandName);
      if (!(property != (PropertyInfo) null) || !(property.GetValue(dataContext) is ICommand command) || !command.CanExecute((object) args))
        return;
      command.Execute((object) args);
    }
  }
}
