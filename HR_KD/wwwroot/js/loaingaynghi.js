// loai-ngay-nghi.js - JavaScript cho các tính năng tương tác với Loại Ngày Nghỉ
$(document).ready(function () {
    // Khởi tạo DataTable với ngôn ngữ tiếng Việt
    var table = $('#loaiNgayNghiTable').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Vietnamese.json'
        },
        responsive: true,
        columnDefs: [
            { orderable: false, targets: -1 } // Cột cuối cùng (thao tác) không được sắp xếp
        ],
        order: [[0, 'asc']] // Sắp xếp theo mã loại ngày nghỉ mặc định
    });

    // Khởi tạo tooltip
    $('[data-toggle="tooltip"]').tooltip();

    // Tự động đóng thông báo sau 5 giây
    setTimeout(function () {
        $(".alert").alert('close');
    }, 5000);

    // Xác nhận xóa
    $('.btn-delete').on('click', function (e) {
        if (!confirm('Bạn có chắc chắn muốn xóa loại ngày nghỉ này?')) {
            e.preventDefault();
        }
    });

    // Chức năng thêm mới nhanh bằng AJAX
    $('#quickAddForm').on('submit', function (e) {
        e.preventDefault();
        var formData = $(this).serialize();

        $.ajax({
            url: '/api/LoaiNgayNghi',
            type: 'POST',
            data: formData,
            success: function (result) {
                // Hiển thị thông báo thành công
                toastr.success('Thêm loại ngày nghỉ thành công!');

                // Làm mới bảng hoặc thêm hàng mới vào bảng
                setTimeout(function () {
                    window.location.reload();
                }, 1000);
            },
            error: function (xhr) {
                // Xử lý lỗi
                var errorMsg = xhr.responseJSON && xhr.responseJSON.message
                    ? xhr.responseJSON.message
                    : 'Đã xảy ra lỗi khi thêm loại ngày nghỉ!';
                toastr.error(errorMsg);
            }
        });
    });

    // Tìm kiếm nâng cao
    $('#advancedSearchBtn').on('click', function () {
        var tenLoai = $('#searchTenLoai').val().toLowerCase();
        var huongLuong = $('#searchHuongLuong').val();
        var tinhVaoPhepNam = $('#searchTinhVaoPhepNam').val();

        // Custom search DataTables
        $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
            var match = true;

            // Tìm theo tên loại
            if (tenLoai && data[1].toLowerCase().indexOf(tenLoai) === -1) {
                match = false;
            }

            // Tìm theo hưởng lương
            if (huongLuong !== '') {
                var isHuongLuong = data[3].indexOf('Có') !== -1;
                if ((huongLuong === 'true' && !isHuongLuong) ||
                    (huongLuong === 'false' && isHuongLuong)) {
                    match = false;
                }
            }

            // Tìm theo tính vào phép năm
            if (tinhVaoPhepNam !== '') {
                var isTinhVaoPhepNam = data[4].indexOf('Có') !== -1;
                if ((tinhVaoPhepNam === 'true' && !isTinhVaoPhepNam) ||
                    (tinhVaoPhepNam === 'false' && isTinhVaoPhepNam)) {
                    match = false;
                }
            }

            return match;
        });

        // Vẽ lại bảng với kết quả tìm kiếm
        table.draw();

        // Xóa hàm tìm kiếm tùy chỉnh
        $.fn.dataTable.ext.search.pop();
    });

    // Reset form tìm kiếm nâng cao
    $('#resetSearchBtn').on('click', function () {
        $('#searchTenLoai').val('');
        $('#searchHuongLuong').val('');
        $('#searchTinhVaoPhepNam').val('');
        table.search('').columns().search('').draw();
    });
});