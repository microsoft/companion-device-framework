using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Identity.Provider;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CDFSampleApp_Ashish
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        String m_selectedDeviceId = String.Empty;
        bool taskRegistered = false;
        static string myBGTaskName = "myBGTask";
        static string myBGTaskEntryPoint = "Tasks.myBGTask";

        public MainPage()
        {
            this.InitializeComponent();

            DeviceListBox.SelectionChanged += DeviceListBox_SelectionChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            IReadOnlyList<SecondaryAuthenticationFactorInfo> deviceList = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope.User);

            RefreshDeviceList(deviceList);

        }

        void RefreshDeviceList(IReadOnlyList<SecondaryAuthenticationFactorInfo> deviceList)
        {
            DeviceListBox.Items.Clear();

            for (int index = 0; index < deviceList.Count; ++index)
            {
                SecondaryAuthenticationFactorInfo deviceInfo = deviceList.ElementAt(index);
                DeviceListBox.Items.Add(deviceInfo.DeviceId);
            }
        }

        private async void RegisterDevice_Click(object sender, RoutedEventArgs e)
        {
            String deviceId = System.Guid.NewGuid().ToString();

            // WARNING: Test code
            // These keys should be generated on the companion device
            // Create device key and authentication key
            IBuffer deviceKey = CryptographicBuffer.GenerateRandom(32);
            IBuffer authKey = CryptographicBuffer.GenerateRandom(32);

            //
            // WARNING: Test code
            // The keys SHOULD NOT be saved into device config data
            //
            byte[] deviceKeyArray = { 0 };
            CryptographicBuffer.CopyToByteArray(deviceKey, out deviceKeyArray);

            byte[] authKeyArray = { 0 };
            CryptographicBuffer.CopyToByteArray(authKey, out authKeyArray);

            //Generate combinedDataArray
            int combinedDataArraySize = deviceKeyArray.Length + authKeyArray.Length;
            byte[] combinedDataArray = new byte[combinedDataArraySize];
            for (int index = 0; index < deviceKeyArray.Length; index++)
            {
                combinedDataArray[index] = deviceKeyArray[index];
            }
            for (int index = 0; index < authKeyArray.Length; index++)
            {
                combinedDataArray[deviceKeyArray.Length + index] = authKeyArray[index];
            }

            // Get a Ibuffer from combinedDataArray
            IBuffer deviceConfigData = CryptographicBuffer.CreateFromByteArray(combinedDataArray);

            //
            // WARNING: Test code
            // The friendly name and device model number SHOULD come from device
            //
            String deviceFriendlyName = "Test Simulator";
            String deviceModelNumber = "Sample A1";

            SecondaryAuthenticationFactorDeviceCapabilities capabilities = SecondaryAuthenticationFactorDeviceCapabilities.SecureStorage;

            SecondaryAuthenticationFactorRegistrationResult registrationResult = await SecondaryAuthenticationFactorRegistration.RequestStartRegisteringDeviceAsync(deviceId,
                    capabilities,
                    deviceFriendlyName,
                    deviceModelNumber,
                    deviceKey,
                    authKey);

            if (registrationResult.Status != SecondaryAuthenticationFactorRegistrationStatus.Started)
            {
                MessageDialog myDlg = null;
                
                if (registrationResult.Status == SecondaryAuthenticationFactorRegistrationStatus.DisabledByPolicy)
                {
                    //For DisaledByPolicy Exception:Ensure secondary auth is enabled.
                    //Use GPEdit.msc to update group policy to allow secondary auth
                    //Local Computer Policy\Computer Configuration\Administrative Templates\Windows Components\Microsoft Secondary Authentication Factor\Allow Companion device for secondary authentication
                    myDlg = new MessageDialog("Disabled by Policy.  Please update the policy and try again.");
                }

                if (registrationResult.Status == SecondaryAuthenticationFactorRegistrationStatus.PinSetupRequired)
                {
                    //For PinSetupRequired Exception:Ensure PIN is setup on the device
                    //Either use gpedit.msc or set reg key
                    //This setting can be enabled by creating the AllowDomainPINLogon REG_DWORD value under the HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System Registry key and setting it to 1.
                    myDlg = new MessageDialog("Please setup PIN for your device and try again.");
                }

                if (myDlg != null)
                {
                    await myDlg.ShowAsync();
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("Device Registration Started!");
            await registrationResult.Registration.FinishRegisteringDeviceAsync(deviceConfigData);

            DeviceListBox.Items.Add(deviceId);
            System.Diagnostics.Debug.WriteLine("Device Registration is Complete!");

            IReadOnlyList<SecondaryAuthenticationFactorInfo> deviceList = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(
                SecondaryAuthenticationFactorDeviceFindScope.User);

            RefreshDeviceList(deviceList);
        }

        private void DeviceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceListBox.Items.Count > 0)
            {
                m_selectedDeviceId = DeviceListBox.SelectedItem.ToString();
            }
            else
            {
                m_selectedDeviceId = String.Empty;
            }
            System.Diagnostics.Debug.WriteLine("The device " + m_selectedDeviceId + " is selected.");

            //Store the selected device in settings to be used in the BG task
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["SelectedDevice"] = m_selectedDeviceId;

        }

        private async void UnregisterDevice_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectedDeviceId == String.Empty)
            {
                return;
            }

            //InfoList.Items.Add("Unregister a device:");

            await SecondaryAuthenticationFactorRegistration.UnregisterDeviceAsync(m_selectedDeviceId);

            //InfoList.Items.Add("Device unregistration is completed.");

            IReadOnlyList<SecondaryAuthenticationFactorInfo> deviceList = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(
                SecondaryAuthenticationFactorDeviceFindScope.User);

            RefreshDeviceList(deviceList);
        }


        void RegisterBgTask_Click(object sender, RoutedEventArgs e)
        {
            RegisterTask();
        }


        private async void OnBgTaskProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            // WARNING: Test code
            // Handle background task progress.
            if (args.Progress == 1)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    System.Diagnostics.Debug.WriteLine("Background task is started.");
                });
            }
        }

        async void RegisterTask()
        {
            System.Diagnostics.Debug.WriteLine("Register the background task.");
            //
            // Check for existing registrations of this background task.
            //

            BackgroundExecutionManager.RemoveAccess();
            var access = await BackgroundExecutionManager.RequestAccessAsync();

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == myBGTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {

                if (access == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
                {
                    BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                    taskBuilder.Name = myBGTaskName;
                    // Create the trigger.
                    SecondaryAuthenticationFactorAuthenticationTrigger myTrigger = new SecondaryAuthenticationFactorAuthenticationTrigger();

                    taskBuilder.TaskEntryPoint = myBGTaskEntryPoint;
                    taskBuilder.SetTrigger(myTrigger);
                    BackgroundTaskRegistration taskReg = taskBuilder.Register();

                    String taskRegName = taskReg.Name;
                    //taskReg.Progress += OnBgTaskProgress;
                    System.Diagnostics.Debug.WriteLine("Background task registration is completed.");
                    taskRegistered = true;
                }
            }

        }
    }
}