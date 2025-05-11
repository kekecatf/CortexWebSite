function toggleDeliveryOption() {
    const shippingDetails = document.getElementById("shipping-details");
    const isClickAndCollect = document.querySelector('input[name="deliveryOption"]:checked').value === "clickAndCollect";
    
    if (isClickAndCollect) {
        shippingDetails.style.display = "none";
        document.getElementById("RecipientName").required = false;
        document.getElementById("ShippingCompany").required = false;
        document.getElementById("Address").required = false;
        document.getElementById("PhoneNumber").required = false;
    } else {
        shippingDetails.style.display = "block";
        document.getElementById("RecipientName").required = true;
        document.getElementById("ShippingCompany").required = true;
        document.getElementById("Address").required = true;
        document.getElementById("PhoneNumber").required = true;
    }
}

function submitCheckoutWithOptions() {
    const isClickAndCollect = document.querySelector('input[name="deliveryOption"]:checked').value === "clickAndCollect";
    
    const formData = {
        IsClickAndCollect: isClickAndCollect,
        CardholderName: document.getElementById("CardholderName").value,
        CardInfo: document.getElementById("CardInfo").value,
        ExpirationDate: document.getElementById("ExpirationDate").value,
        CVV: document.getElementById("CVV").value
    };
    
    if (!isClickAndCollect) {
        formData.RecipientName = document.getElementById("RecipientName").value;
        formData.ShippingCompany = document.getElementById("ShippingCompany").value;
        formData.Address = document.getElementById("Address").value;
        formData.PhoneNumber = document.getElementById("PhoneNumber").value;
    }

    // Client-side validation
    if (!isClickAndCollect) {
        if (!formData.RecipientName || !formData.RecipientName.trim()) {
            alert("Please enter the recipient's name.");
            return;
        }
        if (!formData.ShippingCompany || !formData.ShippingCompany.trim()) {
            alert("Please select a shipping company.");
            return;
        }
        if (!formData.Address || !formData.Address.trim()) {
            alert("Please provide your delivery address.");
            return;
        }
        if (!formData.PhoneNumber || !formData.PhoneNumber.trim()) {
            alert("Please provide your phone number.");
            return;
        }
    }
    
    if (!formData.CardholderName || !formData.CardholderName.trim()) {
        alert("Please provide the cardholder's name.");
        return;
    }
    if (!formData.CardInfo || !formData.CardInfo.trim() || !/^\d{16}$/.test(formData.CardInfo.replace(/-/g, ""))) {
        alert("Please provide a valid 16-digit card number.");
        return;
    }
    if (!formData.ExpirationDate || !formData.ExpirationDate.trim() || !/^\d{2}\/\d{2}$/.test(formData.ExpirationDate)) {
        alert("Please provide a valid expiration date in MM/YY format.");
        return;
    }
    if (!formData.CVV || !formData.CVV.trim() || !/^\d{3}$/.test(formData.CVV)) {
        alert("Please provide a valid 3-digit CVV code.");
        return;
    }

    $.ajax({
        url: checkoutUrl,
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                alert("Your order has been successfully placed. Thank you for shopping with us!");
                window.location.href = cartIndexUrl;
            } else {
                alert("Unable to place your order: " + response.message);
            }
        },
        error: function () {
            alert("An error occurred while completing your checkout. Please try again later.");
        }
    });
} 