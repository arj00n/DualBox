#include <initguid.h>
#include "Device.h"
#include "HidReport.h"
#include "Queue.h"

static NTSTATUS
DualBoxCreateVhfDevice(
    _In_ WDFDEVICE Device
    )
{
    PDEVICE_CONTEXT context;
    VHF_CONFIG vhfConfig;
    NTSTATUS status;

    context = DeviceGetContext(Device);

    VHF_CONFIG_INIT(
        &vhfConfig,
        WdfDeviceWdmGetDeviceObject(Device),
        sizeof(DualBoxHidReportDescriptor),
        (PUCHAR)DualBoxHidReportDescriptor);

    vhfConfig.VhfClientContext = Device;
    vhfConfig.EvtVhfAsyncOperationWriteReport = DualBoxEvtVhfWriteReport;

    status = VhfCreate(&vhfConfig, &context->VhfHandle);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = VhfStart(context->VhfHandle);
    if (!NT_SUCCESS(status)) {
        VhfDelete(context->VhfHandle, TRUE);
        context->VhfHandle = WDF_NO_HANDLE;
    }

    return status;
}

NTSTATUS
DualBoxEvtDeviceAdd(
    _In_ WDFDRIVER Driver,
    _Inout_ PWDFDEVICE_INIT DeviceInit
    )
{
    WDF_OBJECT_ATTRIBUTES attributes;
    WDFDEVICE device;
    PDEVICE_CONTEXT context;
    NTSTATUS status;

    UNREFERENCED_PARAMETER(Driver);

    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_UNKNOWN);
    WdfDeviceInitSetExclusive(DeviceInit, FALSE);

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, DEVICE_CONTEXT);
    attributes.EvtCleanupCallback = DualBoxEvtDeviceContextCleanup;

    status = WdfDeviceCreate(&DeviceInit, &attributes, &device);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    context = DeviceGetContext(device);
    context->VhfHandle = WDF_NO_HANDLE;
    context->LastFeedback.ReportId = DUALBOX_FEEDBACK_REPORT_ID;

    status = WdfWaitLockCreate(WDF_NO_OBJECT_ATTRIBUTES, &context->FeedbackLock);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = WdfDeviceCreateDeviceInterface(
        device,
        &GUID_DEVINTERFACE_DUALBOX_VIRTUAL_PAD,
        NULL);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    status = DualBoxQueueInitialize(device);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    return DualBoxCreateVhfDevice(device);
}

VOID
DualBoxEvtDeviceContextCleanup(
    _In_ WDFOBJECT DeviceObject
    )
{
    PDEVICE_CONTEXT context;

    context = DeviceGetContext(DeviceObject);

    if (context->VhfHandle != WDF_NO_HANDLE) {
        VhfDelete(context->VhfHandle, TRUE);
        context->VhfHandle = WDF_NO_HANDLE;
    }
}

VOID
DualBoxEvtVhfWriteReport(
    _In_ PVOID VhfClientContext,
    _In_ VHFOPERATIONHANDLE VhfOperationHandle,
    _In_opt_ PVOID VhfOperationContext,
    _In_ PHID_XFER_PACKET HidTransferPacket
    )
{
    WDFDEVICE device;
    PDEVICE_CONTEXT context;
    NTSTATUS status;

    UNREFERENCED_PARAMETER(VhfOperationContext);

    device = (WDFDEVICE)VhfClientContext;
    context = DeviceGetContext(device);
    status = STATUS_INVALID_PARAMETER;

    if (HidTransferPacket != NULL &&
        HidTransferPacket->reportBuffer != NULL &&
        HidTransferPacket->reportBufferLen >= sizeof(DUALBOX_FEEDBACK_REPORT) &&
        HidTransferPacket->reportBuffer[0] == DUALBOX_FEEDBACK_REPORT_ID) {

        WdfWaitLockAcquire(context->FeedbackLock, NULL);
        RtlCopyMemory(
            &context->LastFeedback,
            HidTransferPacket->reportBuffer,
            sizeof(DUALBOX_FEEDBACK_REPORT));
        WdfWaitLockRelease(context->FeedbackLock);
        status = STATUS_SUCCESS;
    }

    VhfAsyncOperationComplete(VhfOperationHandle, status);
}
