# BLEConnectController
MicroBitに搭載しているBluetoothLowEnergyのLEDサービスとボタンサービスと連携してWebhookにPOSTするプログラム 
 
# 機能 
IFTTTのWebhookへのPOST  
MicroBitを基準に考えてるため、各種BLEサービスのUUID等はMicroBitの物になってます。  
webhookKeyはIFTTTのKeyを指定し、bleDeviceMacIdは接続したいBLEデバイスのMACアドレスを指定します。  
# 注意 
MicroBit側が悪いのか分かりませんが一定時間たつとBLEサービスイベントの通知を受信できなくなります。  
接続は切れてないようですが原因はよく分かっていません。  
またMicroBit側でペアリング無しで接続できるようにしておく必要があります。  
