#include "Device.h"
#include "Queue.h"

NTSTATUS
DualBoxQueueInitialize(
    _In_ WDFDEVICE Device
    )
{
    WDF_IO_QUEUE_CONFIG queueConfig;

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(
        &queueConfig,
        WdfIoQueueDispatchSequential);

    queueConfig.EvtIoDeviceControl = DualBoxEvtIoDeviceControl;

    return WdfIoQueueCreate(
        Device,
        &queueConfig,
        WDF_NO_OBJECT_ATTRIBUTES,
        WDF_NO_HANDLE);
}

VOID
DualBoxEvtIoDeviceControl(
    _In_ WDFQUEUE Queue,
    _In_ WDFREQUEST Request,
    _In_ size_t OutputBufferLength,
    _In_ size_t InputBufferLength,
    _In_ ULONG IoControlCode
    )
{
    WDFDEVICE device;
    PDEVICE_CONTEXT context;
    NTSTATUS status;
    size_t bytesReturned;

    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);

    device = WdfIoQueueGetDevice(Queue);
    context = DeviceGetContext(device);
    status = STATUS_INVALID_DEVICE_REQUEST;
    bytesReturned = 0;

    switch (IoControlCode) {
    case IOCTL_DUALBOX_SUBMIT_INPUT:
    {
        PDUALBOX_INPUT_REPORT inputReport;
        HID_XFER_PACKET packet;

        status = WdfRequestRetrieveInputBuffer(
            Request,
            sizeof(DUALBOX_INPUT_REPORT),
            (PVOID*)&inputReport,
            NULL);

        if (NT_SUCCESS(status)) {
            if (inputReport->ReportId != DUALBOX_INPUT_REPORT_ID) {
                status = STATUS_INVALID_PARAMETER;
            } else {
                RtlZeroMemory(&packet, sizeof(packet));
                packet.reportBuffer = (PUCHAR)inputReport;
                packet.reportBufferLen = sizeof(DUALBOX_INPUT_REPORT);
                packet.reportId = DUALBOX_INPUT_REPORT_ID;
                status = VhfReadReportSubmit(context->VhfHandle, &packet);
            }
        }
        break;
    }

    case IOCTL_DUALBOX_GET_FEEDBACK:
    {
        PDUALBOX_FEEDBACK_REPORT feedbackReport;

        status = WdfRequestRetrieveOutputBuffer(
            Request,
            sizeof(DUALBOX_FEEDBACK_REPORT),
            (PVOID*)&feedbackReport,
            NULL);

        if (NT_SUCCESS(status)) {
            WdfWaitLockAcquire(context->FeedbackLock, NULL);
            RtlCopyMemory(
                feedbackReport,
                &context->LastFeedback,
                sizeof(DUALBOX_FEEDBACK_REPORT));
            WdfWaitLockRelease(context->FeedbackLock);
            bytesReturned = sizeof(DUALBOX_FEEDBACK_REPORT);
        }
        break;
    }

    default:
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }

    WdfRequestCompleteWithInformation(Request, status, bytesReturned);
}
