using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Torch.Collections;
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.ViewModels.Blocks
{
    public class BlockViewModel : EntityViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public IMyTerminalBlock Block => (IMyTerminalBlock) Entity;
        public MtObservableList<PropertyViewModel> Properties { get; } = new MtObservableList<PropertyViewModel>();

        public string FullName => $"{Block?.CubeGrid.CustomName} - {Block?.CustomName}";

        public override string Name
        {
            get => Block?.CustomName ?? "null";
            set
            {
                TorchBase.Instance.Invoke(() =>
                {
                    Block.CustomName = value;
                    OnPropertyChanged();
                }); 
            }
        }

        /// <inheritdoc />
        public override string Position { get => base.Position; set { } }

        public long BuiltBy
        {
            get => ((MySlimBlock)Block?.SlimBlock)?.BuiltBy ?? 0;
            set
            {
                TorchBase.Instance.Invoke(() =>
                {
                    ((MySlimBlock)Block.SlimBlock).TransferAuthorship(value);
                    OnPropertyChanged();
                });
            }
        }

        public override bool CanStop => false;

        /// <inheritdoc />
        public override void Delete()
        {
            Block.CubeGrid.RazeBlock(Block.Position);
        }

        public BlockViewModel(IMyTerminalBlock block, EntityTreeViewModel tree) : base(block, tree)
        {
            Block?.GetProperties(null, WrapProperty);
        }

        private bool WrapProperty(ITerminalProperty prop)
        {
            try
            {
                Type propType = null;
                foreach (var iface in prop.GetType().GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITerminalProperty<>))
                        propType = iface.GenericTypeArguments[0];
                }

                if (propType == null)
                {
                    Log.Error($"Unable to determine value type for terminal property {prop.Id} ({prop.TypeName})");
                    return false;
                }

                var modelType = typeof(PropertyViewModel<>).MakeGenericType(propType);
                Properties.Add((PropertyViewModel)Activator.CreateInstance(modelType, prop, this));
            }
            catch (Exception e)
            {
                Log.Error($"Exception when wrapping terminal property {prop.Id} ({prop.TypeName})");
                Log.Error(e);
            }

            return false;
        }

        public BlockViewModel()
        {
            
        }
    }
}
