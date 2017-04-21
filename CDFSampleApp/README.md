# CDF Sample App

The sample demonstrates how you can use the CDF APIs to build a UWP app to unlock a Windows PC using a companion device.  For the sake of simplicity and ease, the sample does a lot of operations (like key generation, HMAC operations, etc.) in this sample itself on the PC i.e., making the PC itself as the companion device.   When you implement your own UWP app, please use a transport (like BT, NFC, etc.) to talk to a companion device and have the companion device do some of these operations especially the key generaiton, key storage and HMAC operation.

In the Sample
-------------
The sample has two components: foreground and background task.   

1. The foreground component registers the background task and registers the companion device to be used for Windows unlock.  

2. The background component does the actual auth operation and hence, performs the unlock.

Before using the sample
-----------------------
When you use the sample code, please do the following:

1. Setup PIN auth/PIN based logon on your PC  
  You should use `Settings app-> Accounts -> Sign-in Options` to set the PIN for your PC.
    - If for some reason, PIN based logon is disabled on your PC, you may be able to either use gpedit.msc or set reg key. This setting can be enabled by creating the `AllowDomainPINLogon` REG_DWORD value under the `HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System Registry` key and setting it to 1.
