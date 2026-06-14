#pragma once

#include <ntddk.h>
#include <wdf.h>
#include <vhf.h>
#include "Public.h"

typedef struct _DEVICE_CONTEXT {
    VHFHANDLE VhfHandle;
    WDFWAITLOCK FeedbackLock;
    DUALBOX_FEEDBACK_REPORT LastFeedback;
} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext)

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD DualBoxEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP DualBoxEvtDriverContextCleanup;
EVT_WDF_OBJECT_CONTEXT_CLEANUP DualBoxEvtDeviceContextCleanup;

EVT_VHF_ASYNC_OPERATION DualBoxEvtVhfWriteReport;

