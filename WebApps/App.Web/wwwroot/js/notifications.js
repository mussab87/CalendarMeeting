// Notification System - Main JavaScript File
(function () {
    'use strict';

    let notificationCheckInterval = null;
    let meetingReminderInterval = null;
    let currentMeetingId = null;

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function () {

        initializeNotifications();
        initializeMeetingReminders();

    });

    // Initialize notification system
    function initializeNotifications() {
        // Load initial notification count
        loadNotificationCount();

        // Load notifications when dropdown opens
        //console.log('Setting up dropdown event listener...');
        $('#notificationDropdownToggle').parent().on('show.bs.dropdown', function () {
            //console.log('Dropdown opening - loading notifications...');
            loadNotifications();
        });

        // Also try alternative event
        $('#notificationDropdownToggle').on('click', function () {
            //console.log('Notification bell clicked directly...');
            setTimeout(function () {
                //console.log('Loading notifications after click...');
                loadNotifications();
            }, 100);
        });

        // Mark all as read handler - use event delegation since button is dynamically created
        $(document).on('click', '#markAllAsReadBtn', function (e) {
            e.preventDefault();
            //console.log('Mark all as read button clicked');
            markAllAsRead();
        });

        // Handle modal close buttons - use event delegation for dynamically created content
        $(document).on('click', '#meetingResponseModal .close, #meetingResponseModal [data-dismiss="modal"]', function (e) {
            e.preventDefault();
            $('#meetingResponseModal').modal('hide');
        });

        // Check for new notifications every 30 seconds
        notificationCheckInterval = setInterval(function () {
            loadNotificationCount();
        }, 30000);
    }

    // Initialize meeting reminders
    function initializeMeetingReminders() {
        // Check immediately on load
        checkMeetingReminders();

        // Check every 5 minutes
        meetingReminderInterval = setInterval(function () {
            checkMeetingReminders();
        }, 300000); // 5 minutes
    }

    // Load notification count
    function loadNotificationCount() {
        $.ajax({
            url: '/Notifications/GetUnreadCount',
            type: 'GET',
            success: function (response) {
                updateNotificationBadge(response.count);
            },
            error: function (xhr, status, error) {
                console.error('Error loading notification count:', error);
            }
        });
    }

    // Update notification badge
    function updateNotificationBadge(count) {
        const badge = $('#notificationCount');
        const countText = $('#unreadCountText');

        if (count > 0) {
            badge.text(count > 99 ? '99+' : count);
            badge.show();
            countText.text(count);
        } else {
            badge.hide();
            countText.text('0');
        }
    }

    // Load notifications list
    function loadNotifications() {
        //console.log('Loading notifications...');
        $.ajax({
            url: '/Notifications/GetNotifications',
            type: 'GET',
            data: { unreadOnly: false },
            success: function (response) {
                //console.log('Notifications response:', response);
                if (response.success) {
                    // Filter notifications based on smart logic
                    const filteredNotifications = filterNotificationsByStatus(response.notifications);
                    renderNotifications(filteredNotifications);
                } else {
                    console.error('Response not successful:', response);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading notifications:', error);
                console.error('XHR:', xhr);
                showErrorInNotificationList('فشل تحميل الإشعارات');
            }
        });
    }

    // Smart filtering based on notification type and user response
    function filterNotificationsByStatus(notifications) {
        if (!notifications || notifications.length === 0) {
            return [];
        }

        return notifications.filter(notification => {
            // For Invitation notifications: show until user responds (Accept/Reject) OR until marked as read
            if (notification.notificationType === 'Invitation') {
                // Check if user has responded to this meeting from MeetingParticipants table
                // ResponseStatusEnum: 0 = Pending, 1 = Accepted, 2 = Declined
                if (notification.participantResponseStatus !== null && notification.participantResponseStatus !== undefined) {
                    // For organizers: show only if not read (they created the meeting but should hide after reading)
                    // For regular participants: only show if pending
                    if (notification.isOrganizer) {
                        return !notification.isRead; // Show only if not read for organizers
                    }
                    return notification.participantResponseStatus === 0; // Only show if Pending for non-organizers
                }
                return !notification.isRead; // Show only if not read if no response status available
            }

            // For Update notifications: show until marked as read OR if user hasn't responded to invitation
            if (notification.notificationType === 'Update') {
                // If user has pending response (0), show the notification even if read
                // This allows users to respond to invitations even after meeting updates
                if (notification.participantResponseStatus === 0) {
                    return true; // Always show if pending response
                }
                // Otherwise, show only if unread (normal update behavior)
                return !notification.isRead;
            }

            // For Reminder notifications: show until meeting time passes or dismissed
            if (notification.notificationType === 'Reminder') {
                if (notification.isRead) return false; // Hide if dismissed

                // Hide if meeting time has passed
                if (notification.meetingStartTime) {
                    const meetingTime = new Date(notification.meetingStartTime);
                    const now = new Date();
                    return meetingTime > now; // Show only if meeting hasn't started yet
                }
                return true;
            }

            // For other notification types: show until marked as read
            return !notification.isRead;
        });
    }

    // Get user response status badge based on actual participant status
    function getUserResponseStatus(notification) {
        // For invitation notifications, check actual participant response from DB
        if (notification.notificationType === 'Invitation' && notification.participantResponseStatus !== null && notification.participantResponseStatus !== undefined) {
            // For organizers, show a special badge
            if (notification.isOrganizer) {
                return '<span class="badge badge-primary badge-pill"><i class="flaticon2-user mr-1"></i>منظم الاجتماع</span>';
            }
            
            // ResponseStatusEnum: 0 = Pending, 1 = Accepted, 2 = Declined
            switch (notification.participantResponseStatus) {
                case 0: // Pending
                    return '<span class="badge badge-warning badge-pill"><i class="flaticon2-time mr-1"></i>في انتظار الرد</span>';
                case 1: // Accepted
                    return '<span class="badge badge-success badge-pill"><i class="flaticon2-check-mark mr-1"></i>تمت الموافقة</span>';
                case 2: // Declined
                    return '<span class="badge badge-danger badge-pill"><i class="flaticon2-cancel mr-1"></i>تم الرفض</span>';
                default:
                    return '<span class="badge badge-warning badge-pill"><i class="flaticon2-time mr-1"></i>في انتظار الرد</span>';
            }
        }

        // For other notification types, use the notification type
        switch (notification.notificationType) {
            case 'Invitation':
                return '<span class="badge badge-warning badge-pill"><i class="flaticon2-time mr-1"></i>في انتظار الرد</span>';
            case 'Update':
                return '<span class="badge badge-info badge-pill"><i class="flaticon2-edit mr-1"></i>محدث</span>';
            case 'Reminder':
                return '<span class="badge badge-danger badge-pill"><i class="flaticon2-bell-2 mr-1"></i>تذكير</span>';
            default:
                return '<span class="badge badge-secondary badge-pill">عام</span>';
        }
    }

    // Render notifications
    function renderNotifications(notifications) {
        //console.log('Rendering notifications:', notifications);
        const container = $('#notificationList');
        container.empty();

        if (!notifications || notifications.length === 0) {
            //console.log('No notifications to render');
            container.html(`
                <div class="text-center py-15">
                    <div class="symbol symbol-100 mx-auto mb-5">
                        <span class="symbol-label" style="background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);">
                            <i class="flaticon2-bell-2 icon-3x text-white"></i>
                        </span>
                    </div>
                    <h4 class="text-muted font-weight-bold mb-3">لا توجد إشعارات</h4>
                    <p class="text-muted font-size-sm">ستظهر الإشعارات الجديدة هنا عند توفرها</p>
                </div>
            `);
            // Disable the "Mark All as Read" button when no notifications
            $('#markAllAsReadBtn').prop('disabled', true).addClass('disabled').css('opacity', '0.5');
            return;
        }

        //console.log(`Rendering ${notifications.length} notifications`);
        // Enable the "Mark All as Read" button when notifications exist
        $('#markAllAsReadBtn').prop('disabled', false).removeClass('disabled').css('opacity', '1');

        notifications.forEach(function (notification, index) {
            //console.log(`Creating notification item ${index}:`, notification);
            const notifItem = createNotificationItem(notification);
            container.append(notifItem);
        });

        //console.log('All notification items added to container');
    }

    // Create notification item HTML
    function createNotificationItem(notification) {
        const isUnread = !notification.isRead;
        const bgClass = isUnread ? 'bg-light-primary' : '';
        const timeAgo = formatTimeAgo(notification.createdAt);

        const iconClass = notification.notificationType === 'Invitation' ? 'flaticon2-envelope text-primary' :
            notification.notificationType === 'Update' ? 'flaticon2-notification text-warning' :
                notification.notificationType === 'Reminder' ? 'flaticon2-bell-2 text-info' :
                    'flaticon2-information text-dark';

        // Get user's response status for this meeting (based on participant response status from DB)
        const userStatus = getUserResponseStatus(notification);

        const item = $(`
            <div class="notification-item-wrapper mb-3">
                <div class="notification-item d-flex align-items-center p-4 ${bgClass}" 
                     style="cursor: pointer; border-radius: 12px; border: 1px solid #e1e3ea; transition: all 0.3s ease; box-shadow: 0 2px 4px rgba(0,0,0,0.05);"
                     data-notification-id="${notification.id}"
                     data-meeting-id="${notification.meetingId}"
                     data-notification-type="${notification.notificationType}">
                    
                    <div class="symbol symbol-50 mr-4" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
                        <span class="symbol-label">
                            <i class="${iconClass} icon-xl text-white"></i>
                        </span>
                    </div>
                    
                    <div class="d-flex flex-column flex-grow-1">
                        <div class="d-flex justify-content-between align-items-start mb-2">
                            <div class="font-weight-bold text-dark font-size-lg">${escapeHtml(notification.title)}</div>
                            ${isUnread ? '<span class="badge badge-primary badge-pill">جديد</span>' : ''}
                        </div>
                        
                        <div class="text-muted font-size-sm mb-2 line-height-sm">${escapeHtml(notification.message)}</div>
                        
                        <div class="d-flex justify-content-between align-items-center">
                            <div class="text-muted font-size-xs">
                                <i class="flaticon2-calendar-1 icon-sm mr-1"></i> ${timeAgo}
                            </div>
                            <div class="user-status-badge">
                                ${userStatus}
                            </div>
                        </div>
                        
                        ${notification.meetingStartTime ? `
                            <div class="meeting-time-info mt-2 p-2" style="background: #f8f9fa; border-radius: 6px; border-left: 3px solid #007bff;">
                                <div class="text-primary font-size-xs font-weight-bold">
                                    <i class="flaticon2-time mr-1"></i>
                                    ${formatDateTime(new Date(notification.meetingStartTime))}
                                </div>
                            </div>
                        ` : ''}
                    </div>
                </div>
            </div>
        `);

        // Click handler - try multiple approaches
        //item.on('click', function (e) {
        //    e.preventDefault();
        //    e.stopPropagation();
        //    //console.log('Notification item clicked!', notification);
        //    handleNotificationClick(notification);
        //});

        // Also try direct click handler
        item.click(function (e) {
            e.preventDefault();
            e.stopPropagation();
            //console.log('Direct click handler triggered!', notification);
            handleNotificationClick(notification);
        });

        return item;
    }

    // Handle notification click
    function handleNotificationClick(notification) {
        //console.log('Notification clicked:', notification);

        // Mark as read
        markNotificationAsRead(notification.id);

        // Close dropdown
        $('#notificationDropdownToggle').parent().dropdown('hide');

        // Open meeting details modal
        if (notification.meetingId) {
            //console.log('Opening modal for meeting:', notification.meetingId, 'type:', notification.notificationType);
            showMeetingDetailsModal(notification.meetingId, notification.notificationType);
        } else {
            console.error('No meetingId found in notification:', notification);
        }
    }

    // Mark notification as read
    function markNotificationAsRead(notificationId) {
        debugger
        //console.log('Marking notification as read:', notificationId);
        $.ajax({
            url: '/Notifications/MarkAsRead',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                [window.AntiForgeryHeaderName || 'X-CSRF-TOKEN']: window.AntiForgeryToken
            },
            data: JSON.stringify({ notificationId: notificationId }),
            success: function (response) {
                //console.log('Mark as read response:', response);
                if (response.success) {
                    // Reload count
                    loadNotificationCount();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error marking notification as read:', error);
                console.error('Response:', xhr.responseText);
                console.error('Status:', xhr.status);
            }
        });
    }

    // Mark all as read
    function markAllAsRead() {
        //console.log('markAllAsRead function called');
        $.ajax({
            url: '/Notifications/MarkAllAsRead',
            type: 'GET',
            //contentType: 'application/json',
            //data: JSON.stringify({}),
            success: function (response) {
                //console.log('Mark all as read response:', response);
                if (response.success) {
                    //console.log('Successfully marked all as read, reloading notifications...');
                    loadNotificationCount();
                    loadNotifications();
                    showSuccessToast('تم تحديث جميع الإشعارات');
                } else {
                    console.error('Response not successful:', response);
                    showErrorToast('فشل تحديث الإشعارات');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error marking all as read:', error);
                console.error('XHR:', xhr);
                showErrorToast('فشل تحديث الإشعارات');
            }
        });
    }

    // Show meeting details modal
    function showMeetingDetailsModal(meetingId, notificationType) {
        currentMeetingId = meetingId;

        // Show modal
        $('#meetingResponseModal').modal('show');

        // Add click handler for header close button
        $('#meetingResponseModal .close').off('click').on('click', function() {
            $('#meetingResponseModal').modal('hide');
        });

        // Load meeting details
        loadMeetingDetails(meetingId, notificationType);
    }

    // Load meeting details
    function loadMeetingDetails(meetingId, notificationType) {
        const contentDiv = $('#meetingDetailsContent');
        const actionsDiv = $('#meetingResponseActions');

        // Show loading
        contentDiv.html(`
            <div class="text-center py-10">
                <div class="spinner-border text-primary" role="status">
                    <span class="sr-only">Loading...</span>
                </div>
            </div>
        `);
        actionsDiv.empty();

        $.ajax({
            url: '/Notifications/GetMeetingDetails/' + meetingId,
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    renderMeetingDetails(
                        response.meeting,
                        notificationType,
                        response.userResponseStatus,
                        response.isParticipant,
                        response.isOrganizer || false
                    );
                } else {
                    contentDiv.html(`<div class="alert alert-danger">${response.message}</div>`);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading meeting details:', error);
                contentDiv.html('<div class="alert alert-danger">فشل تحميل تفاصيل الاجتماع</div>');
            }
        });
    }

    // Render meeting details
    function renderMeetingDetails(meeting, notificationType, userResponseStatus, isParticipant, isOrganizer = false) {
        const contentDiv = $('#meetingDetailsContent');
        const actionsDiv = $('#meetingResponseActions');

        const meetingId = meeting.id; // Extract meetingId from meeting object
        const startTime = new Date(meeting.startTime);
        const endTime = new Date(meeting.endTime);

        contentDiv.html(`
            <div class="meeting-details-container">
                <!-- Header with gradient background -->
                <div class="meeting-header mb-6" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; border-radius: 12px; color: white; position: relative; overflow: hidden;">
                    <div style="position: absolute; top: -50px; right: -50px; width: 100px; height: 100px; background: rgba(255,255,255,0.1); border-radius: 50%;"></div>
                    <div style="position: absolute; bottom: -30px; left: -30px; width: 60px; height: 60px; background: rgba(255,255,255,0.1); border-radius: 50%;"></div>
                    
                    <div class="d-flex align-items-center mb-3">
                        <div class="symbol symbol-60 mr-4" style="background: rgba(255,255,255,0.2);">
                            <span class="symbol-label">
                                <i class="flaticon2-calendar-1 icon-xl text-white"></i>
                            </span>
                        </div>
                        <div>
                            <h3 class="text-white font-weight-bold mb-1">${escapeHtml(meeting.title)}</h3>
                            <p class="text-white-50 font-size-sm mb-0">اجتماع منظم بواسطة ${escapeHtml(meeting.organizerName || 'غير محدد')}</p>
                        </div>
                    </div>
                    
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="d-flex align-items-center">
                            <i class="flaticon2-time text-white mr-2"></i>
                            <span class="text-white font-weight-bold">${formatDateTime(startTime)}</span>
                        </div>
                        <div class="notification-type-badge">
                            ${notificationType === 'Invitation' || (notificationType === 'Update' && userResponseStatus === 0) ?
                '<span class="badge badge-warning badge-pill"><i class="flaticon2-time mr-1"></i>دعوة جديدة</span>' :
                '<span class="badge badge-info badge-pill"><i class="flaticon2-edit mr-1"></i>تحديث</span>'
            }
                        </div>
                    </div>
                </div>

                <!-- Meeting Details Cards -->
                <div class="row">
                    <div class="col-md-6 mb-4">
                        <div class="detail-card p-4" style="background: #f8f9fa; border-radius: 12px; border-left: 4px solid #28a745;">
                            <div class="d-flex align-items-center mb-2">
                                <i class="flaticon2-calendar-1 text-success mr-2"></i>
                                <label class="font-weight-bold text-dark mb-0">وقت البداية</label>
                            </div>
                            <p class="text-dark font-size-lg mb-0">${formatDateTime(startTime)}</p>
                        </div>
                    </div>
                    <div class="col-md-6 mb-4">
                        <div class="detail-card p-4" style="background: #f8f9fa; border-radius: 12px; border-left: 4px solid #dc3545;">
                            <div class="d-flex align-items-center mb-2">
                                <i class="flaticon2-calendar-1 text-danger mr-2"></i>
                                <label class="font-weight-bold text-dark mb-0">وقت النهاية</label>
                            </div>
                            <p class="text-dark font-size-lg mb-0">${formatDateTime(endTime)}</p>
                        </div>
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-6 mb-4">
                        <div class="detail-card p-4" style="background: #f8f9fa; border-radius: 12px; border-left: 4px solid #007bff;">
                            <div class="d-flex align-items-center mb-2">
                                <i class="flaticon2-placeholder text-primary mr-2"></i>
                                <label class="font-weight-bold text-dark mb-0">الموقع</label>
                            </div>
                            <p class="text-dark font-size-lg mb-0">${escapeHtml(meeting.location || 'لا يوجد')}</p>
                        </div>
                    </div>
                    <div class="col-md-6 mb-4">
                        <div class="detail-card p-4" style="background: #f8f9fa; border-radius: 12px; border-left: 4px solid #6f42c1;">
                            <div class="d-flex align-items-center mb-2">
                                <i class="flaticon2-user text-purple mr-2"></i>
                                <label class="font-weight-bold text-dark mb-0">المنظم</label>
                            </div>
                            <p class="text-dark font-size-lg mb-0">${escapeHtml(meeting.organizerName || 'غير محدد')}</p>
                        </div>
                    </div>
                </div>

                ${meeting.description ? `
                    <div class="mb-4">
                        <div class="detail-card p-4" style="background: #f8f9fa; border-radius: 12px; border-left: 4px solid #fd7e14;">
                            <div class="d-flex align-items-center mb-3">
                                <i class="flaticon2-file text-warning mr-2"></i>
                                <label class="font-weight-bold text-dark mb-0">وصف الاجتماع</label>
                            </div>
                            <p class="text-dark line-height-lg">${escapeHtml(meeting.description)}</p>
                        </div>
                    </div>
                ` : ''}
            </div>
        `);

        // Add action buttons based on notification type and response status
        actionsDiv.empty();

        // Show Accept/Reject buttons if:
        // 1. Notification is Invitation AND user is not organizer, OR
        // 2. Notification is Update AND user response is Pending (0)
        const showResponseButtons = (notificationType === 'Invitation' && !isOrganizer) ||
            (notificationType === 'Update' && userResponseStatus === 0);

        if (showResponseButtons && isParticipant) {
            // Add message for update notifications with pending response
            if (notificationType === 'Update' && userResponseStatus === 0) {
                contentDiv.prepend(`
                    <div class="alert alert-warning d-flex align-items-center mb-4" role="alert">
                        <i class="flaticon2-information icon-lg mr-3"></i>
                        <div>
                            <strong>تم تحديث الاجتماع</strong> - يرجى الرد على الدعوة
                        </div>
                    </div>
                `);
            }

            // Add reject reason textarea (hidden initially)
            contentDiv.append(`
                <div id="rejectReasonSection" style="display:none;" class="mt-4">
                    <div class="form-group">
                        <label class="font-weight-bold text-dark">سبب الرفض: <span class="text-danger">*</span></label>
                        <textarea id="declineReasonTextarea" class="form-control" rows="3" placeholder="يرجى إدخال سبب رفض الدعوة"></textarea>
                    </div>
                </div>
            `);

            actionsDiv.html(`
                <div class="d-flex justify-content-center w-100">
                    <button type="button" class="btn btn-light-primary btn-lg mr-3 px-6" id="closeModalBtn">
                        <i class="flaticon2-cross mr-2"></i>إغلاق
                    </button>
                    <button type="button" class="btn btn-danger btn-lg mr-3 px-6" id="declineBtn">
                        <i class="flaticon2-cross mr-2"></i>رفض الدعوة
                    </button>
                    <button type="button" class="btn btn-success btn-lg px-6" id="acceptBtn">
                        <i class="flaticon2-check mr-2"></i>قبول الدعوة
                    </button>
                </div>
            `);

            // Close button handler
            $('#closeModalBtn').on('click', function () {
                $('#meetingResponseModal').modal('hide');
            });

            // Accept button handler
            $('#acceptBtn').on('click', function () {
                respondToMeeting(meetingId, 1, null); // 1 = Accepted
            });

            // Decline button handler
            $('#declineBtn').on('click', function () {
                const rejectSection = $('#rejectReasonSection');
                if (rejectSection.is(':visible')) {
                    // Submit decline with reason
                    const reason = $('#declineReasonTextarea').val().trim();
                    if (!reason) {
                        showErrorToast('يرجى إدخال سبب الرفض');
                        return;
                    }
                    respondToMeeting(meetingId, 2, reason); // 2 = Declined
                } else {
                    // Show reject reason section
                    rejectSection.slideDown();
                    $(this).text('تأكيد الرفض');
                }
            });
        } else {
            // For other cases, just show close and view calendar buttons
            actionsDiv.html(`
                <div class="d-flex justify-content-center w-100">
                    <button type="button" class="btn btn-light-primary btn-lg mr-3 px-6" id="closeModalBtn2">
                        <i class="flaticon2-cross mr-2"></i>إغلاق
                    </button>
                    <a href="/Calendar" class="btn btn-primary btn-lg px-6">
                        <i class="flaticon2-calendar-1 mr-2"></i>عرض التقويم
                    </a>
                </div>
            `);

            // Close button handler for non-participant case
            $('#closeModalBtn2').on('click', function () {
                $('#meetingResponseModal').modal('hide');
            });
        }
    }

    // Respond to meeting invitation
    function respondToMeeting(meetingId, responseStatus, declinedReason) {
        debugger
        //console.log('Responding to meeting:', { meetingId, responseStatus, declinedReason });

        const data = {
            meetingId: meetingId,
            responseStatus: responseStatus,
            declinedReason: declinedReason
        };

        $.ajax({
            url: '/Notifications/RespondToMeeting',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                [window.AntiForgeryHeaderName || 'X-CSRF-TOKEN']: window.AntiForgeryToken
            },
            data: JSON.stringify(data),
            success: function (response) {
                //console.log('Respond to meeting response:', response);
                if (response.success) {
                    $('#meetingResponseModal').modal('hide');
                    showSuccessToast(response.message);

                    // Reload notifications
                    loadNotificationCount();

                    // Redirect to calendar after 2 seconds
                    setTimeout(function () {
                        window.location.href = '/Calendar';
                    }, 2000);
                } else {
                    showErrorToast(response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error responding to meeting:', error);
                console.error('Response:', xhr.responseText);
                console.error('Status:', xhr.status);
                showErrorToast('حدث خطأ أثناء الاستجابة للدعوة');
            }
        });
    }

    // Check meeting reminders (today's meetings and upcoming in 30 minutes)
    function checkMeetingReminders() {
        // Check today's meetings
        $.ajax({
            url: '/Notifications/GetTodayMeetings',
            type: 'GET',
            success: function (response) {
                if (response.success && response.meetings && response.meetings.length > 0) {
                    response.meetings.forEach(function (meeting) {
                        showMeetingReminderNotification(meeting, 'today');
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error checking today meetings:', error);
            }
        });

        // Check upcoming meetings (30 minutes)
        $.ajax({
            url: '/Notifications/GetUpcomingMeetings',
            type: 'GET',
            data: { minutesBefore: 30 },
            success: function (response) {
                if (response.success && response.meetings && response.meetings.length > 0) {
                    response.meetings.forEach(function (meeting) {
                        showMeetingReminderNotification(meeting, 'upcoming');
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error checking upcoming meetings:', error);
            }
        });
    }

    // Show meeting reminder notification (browser notification or toast)
    function showMeetingReminderNotification(meeting, type) {
        const title = type === 'today' ? 'لديك اجتماع اليوم' : 'اجتماع قريب';
        const body = `${meeting.title} - ${formatDateTime(new Date(meeting.startTime))}`;

        // Try browser notification
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: '/assets/media/logos/logo-letter-1.png'
            });
        } else if ('Notification' in window && Notification.permission !== 'denied') {
            Notification.requestPermission().then(function (permission) {
                if (permission === 'granted') {
                    new Notification(title, {
                        body: body,
                        icon: '/assets/media/logos/logo-letter-1.png'
                    });
                }
            });
        }

        // Also show toast
        showInfoToast(`${title}: ${body}`);
    }

    // Utility functions
    function formatTimeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now - date) / 1000);

        if (seconds < 60) return 'الآن';
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return `منذ ${minutes} دقيقة`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `منذ ${hours} ساعة`;
        const days = Math.floor(hours / 24);
        if (days < 7) return `منذ ${days} يوم`;
        const weeks = Math.floor(days / 7);
        if (weeks < 4) return `منذ ${weeks} أسبوع`;
        const months = Math.floor(days / 30);
        return `منذ ${months} شهر`;
    }

    function formatDateTime(date) {
        const options = {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        };
        return date.toLocaleDateString('ar-SA', options);
    }

    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    function showSuccessToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": true,
                "positionClass": "toast-top-right",
                "preventDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "15000", // Show for 15 seconds
                "extendedTimeOut": "5000", // Extra time on hover
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut",
                "tapToDismiss": true,
                "rtl": true // Right-to-left for Arabic
            };
            toastr.success(message);
        } else {
            alert(message);
        }
    }

    function showErrorToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": true,
                "positionClass": "toast-top-right",
                "preventDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "20000", // Show for 20 seconds (longer for errors)
                "extendedTimeOut": "5000", // Extra time on hover
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut",
                "tapToDismiss": true,
                "rtl": true // Right-to-left for Arabic
            };
            toastr.error(message);
        } else {
            alert(message);
        }
    }

    function showInfoToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": true,
                "positionClass": "toast-top-right",
                "preventDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "25000", // Show for 25 seconds (longest for info/meeting reminders)
                "extendedTimeOut": "5000", // Extra time on hover
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut",
                "tapToDismiss": true,
                "rtl": true // Right-to-left for Arabic
            };
            toastr.info(message);
        } else {
            alert(message);
        }
    }

    function showErrorInNotificationList(message) {
        $('#notificationList').html(`
            <div class="alert alert-danger m-3">
                <div class="alert-text">${message}</div>
            </div>
        `);
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (notificationCheckInterval) {
            clearInterval(notificationCheckInterval);
        }
        if (meetingReminderInterval) {
            clearInterval(meetingReminderInterval);
        }
    });

})();

