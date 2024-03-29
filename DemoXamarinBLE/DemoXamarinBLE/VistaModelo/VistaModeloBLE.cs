﻿using DemoXamarinBLE.Modelo;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xamarin.Forms;


namespace DemoXamarinBLE.VistaModelo
{
    public class VistaModeloBLE : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // modelo
        private ModeloBLE _modelo;
        public ModeloBLE modelo
        {
            get
            {
                return _modelo;
            }
            set
            {
                _modelo = value;
                OnPropertyChanged("modelo");
            }
        }

        // gestor de BLE
        private IAdapter bleAdapter;
        private IBluetoothLE bleHandler;

        public VistaModeloBLE()
        {
            // este constructor hace todo

            // crea el modelo
            modelo = new ModeloBLE
            {
                EstatusBLE = "",
                ListaCaracteristicas = new System.Collections.ObjectModel.ObservableCollection<Plugin.BLE.Abstractions.Contracts.ICharacteristic>(),
                ListaDispositivos = new System.Collections.ObjectModel.ObservableCollection<Plugin.BLE.Abstractions.Contracts.IDevice>(),
                ListaServicios = new System.Collections.ObjectModel.ObservableCollection<Plugin.BLE.Abstractions.Contracts.IService>(),
                DatosLeidos = ""
            };

            // obteniendo las instancias del hardware ble
            bleHandler = CrossBluetoothLE.Current;
            bleAdapter = CrossBluetoothLE.Current.Adapter;

            // configurando evento inicial del proceso de escaneo de dispositivos 
            // y cambios de estado
            bleHandler.StateChanged += (sender, args) =>
            {
                modelo.EstatusBLE = $"Estado del bluetooth: {args.NewState}";
            };
            
            bleAdapter.ScanMode = ScanMode.LowPower; // se establece que el escaneo de advertising se optimiza para bajo consumo 
            bleAdapter.ScanTimeout = 10000; // tiempo de busqueda de dispositivos en advertising
            bleAdapter.ScanTimeoutElapsed += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("El escaneo se ha terminado");
                modelo.EstatusBLE = $"Estado del bluetooth: {bleHandler.State}";
            };

            // se ejecuta cuando BLE encuentra un dispositivo que esta en advertising
            bleAdapter.DeviceDiscovered += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Se ha descubierto un dispositivo");
                IDevice dispositivoDescubierto = args.Device;

                // buscando en la lista de dispositivos en memoria si ya existe
                //List<IDevice> lstDispositivoRepetido = (from disps in modelo.ListaDispositivos
                //                                        where disps.Name == dispositivoDescubierto.Name
                //                                        select disps).ToList();

                //// no hay repetidos
                //if (!lstDispositivoRepetido.Any())
                //{
                modelo.ListaDispositivos.Add(dispositivoDescubierto);
                //}
            };
    
            bleAdapter.DeviceDisconnected += OnDeviceDisconnected;
            bleAdapter.DeviceConnectionLost += OnDeviceConnectionLost;

            modelo.EstatusBLE = "Listo...";
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
            /*Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();

            _userDialogs.HideLoading();
            _userDialogs.ErrorToast("Error", $"Connection LOST {e.Device.Name}", TimeSpan.FromMilliseconds(6000));*/

            System.Diagnostics.Debug.WriteLine("CONNECTION LOST !!!!");
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e)
        {
            /*Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();
            _userDialogs.HideLoading();
            _userDialogs.Toast($"Disconnected {e.Device.Name}");

            Console.WriteLine($"Disconnected {e.Device.Name}");*/

            System.Diagnostics.Debug.WriteLine("DEVICE DISCONNECTED !!!!");
        }

        // conecta a dispositivo conocido
        private Command _CmdConectaDispositivoConocido;
        public Command CmdConectaDispositivoConocido
        {
            get
            {
                if (_CmdConectaDispositivoConocido == null)
                {
                    _CmdConectaDispositivoConocido = new Command(async () =>
                    {
                        System.Diagnostics.Debug.WriteLine("conectar a dispositivo conocido");

                        //System.Guid address = new System.Guid("D5-07-0E-4F-46-B7");
                        //IDevice disp = await bleAdapter.ConnectToKnownDeviceAsync(System.Guid.Parse("00000000-0000-0000-0000-d5070e4f46b7"));
                        IDevice disp = await bleAdapter.ConnectToKnownDeviceAsync(System.Guid.Parse("00000000-0000-0000-0000-000d6f289f87"));

                        modelo.ListaServicios.Clear();

                        foreach (IService servicio in await disp.GetServicesAsync())
                        {
                            if (servicio.Id.Equals(System.Guid.Parse("85601a0a-82b5-4c25-a107-565ff8402d97")))
                            {
                                System.Diagnostics.Debug.WriteLine("ES EL MISMO ID!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                //servicio.Name.Concat("IKKONEKT");
                                
                            }
                            modelo.ListaServicios.Add(servicio);
                        }

                        // pasar a la siguiente pagina
                        modelo.EstatusBLE = $"Estado del bluetooth: {bleHandler.State}";
                        await ((NavigationPage)App.Current.MainPage).PushAsync(new Vista.VistaServicios(App.vmBle));

                    });
                }

                return _CmdConectaDispositivoConocido;
            }
        }

        private Command _CmdIniciaEscaneo;
        public Command CmdIniciaEscaneo
        {
            get
            {
                if (_CmdIniciaEscaneo == null)
                {
                    _CmdIniciaEscaneo = new Command(async () =>
                    {
                        try
                        {
                            await ((NavigationPage)App.Current.MainPage).PushAsync(new Vista.VistaDispositivos(App.vmBle));
                            if (!bleAdapter.IsScanning)
                            {
                                System.Diagnostics.Debug.WriteLine("Comienza el escaneo");
                                modelo.EstatusBLE = "Escanenado por dispositivos BLE";
                                modelo.ListaDispositivos.Clear();
                                await bleAdapter.StartScanningForDevicesAsync();
                            }
                        }
                        catch (System.Exception ex)
                        {
                            await App.Current.MainPage.DisplayAlert("Demo BLE", ex.Message, "Ok");
                        }
                    });
                }

                return _CmdIniciaEscaneo;
            }
        }

        // conecta el dispositivo
        private Command _CmdConectaDispositivo;
        public Command CmdConectaDispositivo
        {
            get
            {
                if (_CmdConectaDispositivo == null)
                {
                    _CmdConectaDispositivo = new Command(async () =>
                    {
                        if (bleAdapter.IsScanning)
                        {
                            await bleAdapter.StopScanningForDevicesAsync();
                        }
                        modelo.EstatusBLE = "Conectando con el periférico...";

                        //await bleAdapter.ConnectToDeviceAsync(modelo.DispositivoConectado, new Plugin.BLE.Abstractions.ConnectParameters(true,false));
                        await bleAdapter.ConnectToDeviceAsync(modelo.DispositivoConectado);

                        System.Diagnostics.Debug.WriteLine("SE CONECTA!!!!!!!!!!!!!!!!");

                        modelo.ListaServicios.Clear();

                        foreach (IService servicio in await modelo.DispositivoConectado.GetServicesAsync())
                        {
                            modelo.ListaServicios.Add(servicio);
                        }

                        // pasar a la siguiente pagina
                        modelo.EstatusBLE = $"Estado del bluetooth: {bleHandler.State}";
                        await ((NavigationPage)App.Current.MainPage).PushAsync(new Vista.VistaServicios(App.vmBle));
                    });
                }

                return _CmdConectaDispositivo;
            }
        }

        private Command _CmdSeleccionaServicio;
        public Command CmdSeleccionaServicio
        {
            get
            {
                if (_CmdSeleccionaServicio == null)
                {
                    _CmdSeleccionaServicio = new Command(async () =>
                    {
                        modelo.ListaCaracteristicas.Clear();
                        foreach (ICharacteristic characteristic in await modelo.ServicioSeleccionado.GetCharacteristicsAsync())
                        {
                            modelo.ListaCaracteristicas.Add(characteristic);
                        }

                        // cambiar pagina
                        await ((NavigationPage)App.Current.MainPage).PushAsync(new Vista.VistaCaracteristicas(App.vmBle));
                    });
                }

                return _CmdSeleccionaServicio;
            }
        }

        private Command _CmdInteractuarConCaracteristica;
        public Command CmdInteractuarConCaracteristica
        {
            get
            {
                if (_CmdInteractuarConCaracteristica == null)
                {
                    _CmdInteractuarConCaracteristica = new Command(async () =>
                    {
                        // imprimir las capacidades de la caracteristica
                        System.Diagnostics.Debug.WriteLine($"Capcidades de la caracteristica: Lectura {modelo.CaracteristicaSeleccionada.CanRead}, Escritura {modelo.CaracteristicaSeleccionada.CanWrite}, Actualizacion {modelo.CaracteristicaSeleccionada.CanWrite}");

                        // HEMENDIK NOTIFY-ENA 
                        /*
                        //var descriptor = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{00002902-0000-1000-8000-00805f9b34fb}"));
                        var descriptor = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{f1e1a72c-6a1e-4558-ae90-c29d2e20245a}"));

                        if (descriptor != null)
                        {
                           byte[] array2 = { 02, 00};
                           await descriptor.WriteAsync(array2);
                        } else
                        {
                           System.Diagnostics.Debug.WriteLine("No descriptor");
                        }
                                                
                        modelo.CaracteristicaSeleccionada.ValueUpdated += (o, args) =>
                        {
                            var bytes = args.Characteristic.Value;
                            //var result = System.BitConverter.ToInt64(bytes,0);
                            foreach(var b in bytes)
                            {
                                System.Diagnostics.Debug.WriteLine(b);
                            }
                            System.Diagnostics.Debug.WriteLine("UPDATE!!!!!!");
                        };
                        
                        modelo.CaracteristicaSeleccionada.ValueUpdated += (o, args) =>
                        {
                            var bytes = args.Characteristic.Value;
                        };
                        await modelo.CaracteristicaSeleccionada.StartUpdatesAsync();
                       */
                        // HONAINO

                        /*
                           if (modelo.CaracteristicaSeleccionada.CanRead)
                           {
                               byte[] datos = await modelo.CaracteristicaSeleccionada.ReadAsync();

                               System.Diagnostics.Debug.WriteLine($"Datos: {Encoding.UTF8.GetString(datos)}");
                           }

                           if (modelo.CaracteristicaSeleccionada.CanWrite)
                           {
                               string newName = "Ane";
                               byte[] newNameBytes = Encoding.ASCII.GetBytes(newName);
                               await modelo.CaracteristicaSeleccionada.WriteAsync(newNameBytes);

                           }

                           if (modelo.CaracteristicaSeleccionada.CanRead)
                           {
                               byte[] datos = await modelo.CaracteristicaSeleccionada.ReadAsync();

                               System.Diagnostics.Debug.WriteLine($"Datos: {Encoding.UTF8.GetString(datos)}");
                           }
                         */
                        await ((NavigationPage)App.Current.MainPage).PushAsync(new Vista.ReadWritePage(App.vmBle));
                    });
                }

                return _CmdInteractuarConCaracteristica;
            }
        }

        private Command _CmdRead;
        public Command CmdRead
        {
            get
            {
                if (_CmdRead == null)
                {
                    _CmdRead = new Command(async () =>
                    {
                        // imprimir las capacidades de la caracteristica
                        System.Diagnostics.Debug.WriteLine($"Capcidades de la caracteristica: Lectura {modelo.CaracteristicaSeleccionada.CanRead}, Escritura {modelo.CaracteristicaSeleccionada.CanWrite}, Actualizacion {modelo.CaracteristicaSeleccionada.CanWrite}");

                        if (modelo.CaracteristicaSeleccionada.CanRead)
                        {
                            System.Diagnostics.Debug.WriteLine("Hona bai");
                            byte[] datos = await modelo.CaracteristicaSeleccionada.ReadAsync();
                            System.Diagnostics.Debug.WriteLine("Hone be bai");

                            System.Diagnostics.Debug.WriteLine($"Daots en bytes: {datos[0]}");
                            //System.Diagnostics.Debug.WriteLine($"Datos: {Encoding.UTF8.GetString(datos)}");
                            System.Diagnostics.Debug.WriteLine(modelo.DatosLeidos);
                            //modelo.DatosLeidos = Encoding.UTF8.GetString(datos);
                            modelo.DatosLeidos = datos[0].ToString();
                            System.Diagnostics.Debug.WriteLine(modelo.DatosLeidos);
                        }
                        /*
                        if (modelo.CaracteristicaSeleccionada.CanWrite)
                        {
                            string newName = "Ane";
                            byte[] newNameBytes = Encoding.ASCII.GetBytes(newName);
                            await modelo.CaracteristicaSeleccionada.WriteAsync(newNameBytes);

                        }*/
                    });
                }

                return _CmdRead;
            }
        }
        private Command _CmdWrite;
        public Command CmdWrite
        {
            get
            {
                if (_CmdWrite == null)
                {
                    _CmdWrite = new Command(async () =>
                    {
                        // imprimir las capacidades de la caracteristica
                        System.Diagnostics.Debug.WriteLine($"Capcidades de la caracteristica: Lectura {modelo.CaracteristicaSeleccionada.CanRead}, Escritura {modelo.CaracteristicaSeleccionada.CanWrite}, Actualizacion {modelo.CaracteristicaSeleccionada.CanWrite}");

                        System.Diagnostics.Debug.WriteLine(modelo.TextoEnviar);

                        if (modelo.CaracteristicaSeleccionada.CanWrite)
                        {
                            /*string newName = "Ane2";
                            byte[] newNameBytes = Encoding.ASCII.GetBytes(newName);
                            await modelo.CaracteristicaSeleccionada.WriteAsync(newNameBytes);*/
                            //await modelo.CaracteristicaSeleccionada.WriteAsync(Encoding.ASCII.GetBytes(modelo.TextoEnviar));
                            await modelo.CaracteristicaSeleccionada.WriteAsync(System.BitConverter.GetBytes(modelo.TextoEnviar));
                            //System.Diagnostics.Debug.WriteLine($"Bytes: {System.BitConverter.GetBytes(modelo.TextoEnviar)}");
                        }
                    });
                }

                return _CmdWrite;
            }
        }

        private Command _CmdHabilitarNot;
        public Command CmdHabilitarNot
        {
            get
            {
                if (_CmdHabilitarNot == null)
                {
                    _CmdHabilitarNot = new Command(async () =>
                    {
                        //var descriptor = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{f1e1a72c-6a1e-4558-ae90-c29d2e20245a}"));

                        /* modelo.DescriptorSeleccionado = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{f1e1a72c-6a1e-4558-ae90-c29d2e20245a}"));

                         if (modelo.DescriptorSeleccionado != null)
                         {
                             byte[] array2 = { 02, 00 };
                             await modelo.DescriptorSeleccionado.WriteAsync(array2);
                             modelo.DatosNotificacion += "Notificaciones habilitadas\n";
                         }
                         else
                         {
                             System.Diagnostics.Debug.WriteLine("No descriptor");
                         }*/

                        modelo.DatosNotificacion += "Notificaciones habilitadas\n";

                        modelo.CaracteristicaSeleccionada.ValueUpdated += (o, args) =>
                        {
                            var bytes = args.Characteristic.Value;
                            //var result = System.BitConverter.ToInt64(bytes,0);

                            // modelo.DatosNotificacion += "Temperatura: " + bytes[0] + "\n";

                            System.Diagnostics.Debug.WriteLine("NOTIFICACIONES");

                            //   for (int i=0; i<1; i++)
                            //   {
                            //       System.Diagnostics.Debug.WriteLine("Byte 1:");
                            //       System.Diagnostics.Debug.WriteLine(bytes.GetValue(0));
                            //   }
                            System.Diagnostics.Debug.WriteLine("Temperatura: " + bytes.GetValue(0) + "\n");
                            modelo.DatosNotificacion += "Temperatura: " + bytes.GetValue(0) + "\n";

                            //   foreach (var b in bytes)
                            //  {           
                            //       System.Diagnostics.Debug.WriteLine(b);
                            //       modelo.DatosNotificacion += "Temperatura: " + b + "\n";
                            //       System.Diagnostics.Debug.WriteLine("Temperatura: " + b);
                            //   }

                            System.Diagnostics.Debug.WriteLine("UPDATE!!!!!!");
                            
                        };

                       /* modelo.CaracteristicaSeleccionada.ValueUpdated += (o, args) =>
                        {
                            var bytes = args.Characteristic.Value;
                        };*/

                        await modelo.CaracteristicaSeleccionada.StartUpdatesAsync();
                    });
                }

                return _CmdHabilitarNot;
            }
        }

        private Command _CmdDeshabilitarNot;
        public Command CmdDeshabilitarNot
        {
            get
            {
                if (_CmdDeshabilitarNot == null)
                {
                    _CmdDeshabilitarNot = new Command(async () =>
                    {
                        //modelo.DescriptorSeleccionado = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{f1e1a72c-6a1e-4558-ae90-c29d2e20245a}"));

                        await modelo.CaracteristicaSeleccionada.StopUpdatesAsync();

                       // modelo.DescriptorSeleccionado = await modelo.CaracteristicaSeleccionada.GetDescriptorAsync(System.Guid.Parse("{f1e1a72c-6a1e-4558-ae90-c29d2e20245a}"));

                       // byte[] array2 = { 00, 00 };
                       // await modelo.DescriptorSeleccionado.WriteAsync(array2);
                        modelo.DatosNotificacion += "Notificaciones deshabilitadas\n";

                    });
                }

                return _CmdDeshabilitarNot;
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}