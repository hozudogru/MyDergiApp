function showToast(message, isSuccess = true) {
    const toastEl = document.getElementById('liveToast');
    const toastBody = document.getElementById('liveToastBody');

    if (!toastEl || !toastBody || typeof bootstrap === "undefined") return;

    toastBody.textContent = message;

    toastEl.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-dark');
    toastEl.classList.add(isSuccess ? 'text-bg-success' : 'text-bg-danger');

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, {
        delay: 2500
    });

    toast.show();
}

$(function () {
    if ($.fn.DataTable && !$.fn.DataTable.isDataTable('#usersTable')) {
        $('#usersTable').DataTable({
            info: false,
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.8/i18n/tr.json'
            },
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50],
            order: [[4, 'desc']],
            columnDefs: [
                { orderable: false, targets: 5 }
            ]
        });
    }
});

$(document).off('click', '.toggle-status');

$(document).on('click', '.toggle-status', function (e) {
    e.preventDefault();

    const button = $(this);

    if (button.is(':disabled')) return;

    const userId = button.data('id');
    const row = button.closest('tr');
    const badge = row.find('.status-badge');

    button.prop('disabled', true);

    $.ajax({
        url: '/UserManagement/ToggleActive',
        type: 'POST',
        data: { id: userId },
        success: function (res) {
            if (!res.success) {
                showToast(res.message || 'İşlem başarısız', false);
                return;
            }

            if (res.isActive) {
                badge
                    .removeClass('bg-danger bg-secondary')
                    .addClass('bg-success')
                    .text('Aktif');

                button
                    .removeClass('btn-success')
                    .addClass('btn-danger')
                    .html('<i class="bi bi-x-circle"></i> Pasif Yap');
            } else {
                badge
                    .removeClass('bg-success')
                    .addClass('bg-danger')
                    .text('Pasif');

                button
                    .removeClass('btn-danger')
                    .addClass('btn-success')
                    .html('<i class="bi bi-check-circle"></i> Aktif Yap');
            }

            showToast('Kullanıcı durumu güncellendi', true);
        },
        error: function () {
            showToast('Sunucu hatası', false);
        },
        complete: function () {
            button.prop('disabled', false);
        }
    });
});