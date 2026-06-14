#pragma once

#include <wdf.h>

EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL DualBoxEvtIoDeviceControl;

NTSTATUS DualBoxQueueInitialize(_In_ WDFDEVICE Device);

