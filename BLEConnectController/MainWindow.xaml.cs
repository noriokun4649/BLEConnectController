using System;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Net;
using System.Collections;

namespace BLEConnectController
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private DeviceWatcher deviceWatcher;
        private DeviceInformation deviceInfo;
        private GattCharacteristic ledCharacteristic;
        private Hashtable serviceUUID = new Hashtable();
        private Hashtable buttonUUID = new Hashtable();
        private Hashtable ledUUID = new Hashtable();
        private Hashtable uuidToEventname = new Hashtable();
        private const string webhookKey = "";
        private const string bleDeviceMacId = "";

        public MainWindow()
        {
            InitializeComponent();
            serviceUUID.Add("button", "e95d9882-251d-470a-a062-fa1922dfa9a8");
            serviceUUID.Add("led", "e95dd91d-251d-470a-a062-fa1922dfa9a8");
            buttonUUID.Add("buttonA", "e95dda90-251d-470a-a062-fa1922dfa9a8");
            buttonUUID.Add("buttonB", "e95dda91-251d-470a-a062-fa1922dfa9a8");
            ledUUID.Add("ledMatrix", "e95d7b77 -251d-470a-a062-fa1922dfa9a8");
            ledUUID.Add("ledScrollingDelay", "e95d93ee-251d-470a-a062-fa1922dfa9a8");
            uuidToEventname.Add(buttonUUID["buttonA"], "standlight_on");
            uuidToEventname.Add(buttonUUID["buttonB"], "standlight_off");
            LogOutput("Bluetooth Low Energyデバイスの検索開始");
            LogOutput("-----------------------");
            StartWatcher();
        }

        private void Watcher_DeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            LogOutput("検索成功");
            LogOutput("名前:" + deviceInfo.Name);
            LogOutput("ID:" + deviceInfo.Id);
            LogOutput("-----------------------");
            if (deviceInfo.Id.IndexOf(bleDeviceMacId) > 0)
            {
                this.deviceInfo = deviceInfo;
                DeviceSetup(this.deviceInfo.Id);
            }
        }

        private async void DeviceSetup(string id)
        {
            try
            {
                LogOutput(bleDeviceMacId + "のMicroBit検索成功");
                deviceWatcher.Added -= Watcher_DeviceAdded;
                deviceWatcher.Stop();

                var device = await BluetoothLEDevice.FromIdAsync(id);
                LogOutput("Bluetooth Low Energy デバイス情報取得完了");
                device.ConnectionStatusChanged += (d, obj) =>
                {
                    LogOutput("Bluetooth Low Energy Device " + d.ConnectionStatus.ToString());
                    if (d.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                    {
                        LogOutput("Bluetooth Low Energy サービスに再接続開始");
                        ConnectServices(device);
                    }
                };
                ConnectServices(device);
            }
            catch
            {
                LogOutput("デバイスセットアップエラー");
                LogOutput("Bluetooth Low Energy デバイスの再検索開始");
                StartWatcher();
            }
        }
        private async void ConnectServices(BluetoothLEDevice device)
        {
            try
            {
                var buttonUuid = new Guid(serviceUUID["button"].ToString());
                var buttonAChara = new Guid(buttonUUID["buttonA"].ToString());
                var buttonBChara = new Guid(buttonUUID["buttonB"].ToString());
                LogOutput("ボタンサービスへの接続開始");
                var buttonCharaData = await ConnectService(device, buttonUuid, buttonAChara, buttonBChara);
                var buttonAState = await buttonCharaData[0].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                var buttonBState = await buttonCharaData[1].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                LogOutput("ボタンAの変更を通知するように書き込み：" + buttonAState.ToString());
                LogOutput("ボタンBの変更を通知するように書き込み：" + buttonBState.ToString());
                buttonCharaData[0].ValueChanged += ReceiveEventButton;
                buttonCharaData[1].ValueChanged += ReceiveEventButton;

                var ledUuid = new Guid(serviceUUID["led"].ToString());
                var ledChara = new Guid(ledUUID["ledMatrix"].ToString());
                var ledScrollingChara = new Guid(ledUUID["ledScrollingDelay"].ToString());
                LogOutput("LEDサービスへの接続開始");
                var ledCharaData = await ConnectService(device, ledUuid, ledChara, ledScrollingChara);
                ledCharacteristic = ledCharaData[0];

                var ledlist = LEDList(
                    LEDConvert(1, 1, 1, 0, 1),
                    LEDConvert(0, 0, 1, 0, 1),
                    LEDConvert(1, 1, 1, 1, 1),
                    LEDConvert(1, 0, 1, 0, 0),
                    LEDConvert(1, 0, 1, 1, 1));
                await ledCharacteristic.WriteValueAsync(ledlist);
            }
            catch
            {
                LogOutput("デバイスセットアップエラー");
                LogOutput("Bluetooth Low Energy デバイスの再検索開始");
                StartWatcher();
            }
        }
        private void ReceiveEventButton(GattCharacteristic characteristic, GattValueChangedEventArgs changedEventArgs)
        {
            var eventname = uuidToEventname[characteristic.Uuid.ToString()].ToString();
            var read = DataReader.FromBuffer(changedEventArgs.CharacteristicValue);
            var input = new byte[read.UnconsumedBufferLength];
            read.ReadBytes(input);
            LogOutput(input[0] == 0 ? "離した" : "押した");
            if (input[0] == 0)
            {
                WebHookAsync("https://maker.ifttt.com/trigger/" + eventname + "/with/key/" + webhookKey);
            }
        }

        private async Task<GattCharacteristic[]> ConnectService(BluetoothLEDevice device, Guid serviceUUID, Guid characteristicUUID1, Guid characteristicUUID2)
        {
            var service = await device.GetGattServicesForUuidAsync(serviceUUID);
            LogOutput("サービスの取得を完了");
            var characteristics1 = await service.Services[0].GetCharacteristicsForUuidAsync(characteristicUUID1);
            LogOutput("Characteristic 1 の取得完了");
            var characteristics2 = await service.Services[0].GetCharacteristicsForUuidAsync(characteristicUUID2);
            LogOutput("Characteristic 2 の取得完了");
            var characteristics1Data = characteristics1.Characteristics[0];
            var characteristics2Data = characteristics2.Characteristics[0];
            return new GattCharacteristic[] { characteristics1Data, characteristics2Data };
        }

        private void LogOutput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                logs.Items.Add(text);
            });
        }

        private byte LEDConvert(int i1, int i2, int i3, int i4, int i5)
        {
            return Convert.ToByte(i1.ToString() + i2.ToString() + i3.ToString() + i4.ToString() + i5.ToString(), 2);
        }

        private IBuffer LEDList(byte row1, byte row2, byte row3, byte row4, byte row5)
        {
            return new byte[] { row1, row2, row3, row4, row5 }.AsBuffer();
        }

        private async void WebHookAsync(string url)
        {
            LogOutput("WebHookへPOST");
            var httpClient = new HttpClient();
            var strs = new Dictionary<string, string>();
            var awaiter = httpClient.PostAsync(url, new
            FormUrlEncodedContent(strs)).GetAwaiter();
            var result = awaiter.GetResult();
            LogOutput("POST結果：" + result.StatusCode.ToString());
            if (result.StatusCode == HttpStatusCode.OK)
            {
                var ledlist = LEDList(
                    LEDConvert(0, 1, 1, 1, 0),
                    LEDConvert(1, 0, 0, 0, 1),
                    LEDConvert(1, 0, 0, 0, 1),
                    LEDConvert(1, 0, 0, 0, 1),
                    LEDConvert(0, 1, 1, 1, 0));
                await ledCharacteristic.WriteValueAsync(ledlist);
            }
        }

        private void StartWatcher()
        {
            var selector = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            deviceWatcher = DeviceInformation.CreateWatcher(selector, null, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += Watcher_DeviceAdded;
            deviceWatcher.Start();
        }
    }
}
