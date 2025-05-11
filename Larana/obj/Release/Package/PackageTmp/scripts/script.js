function sendEmail(event) {
  event.preventDefault();

  var name = document.getElementById('name').value;
  var email = document.getElementById('email').value;
  var message = document.getElementById('message').value;

  if (name && email && message) {
    document.getElementById('email-status').innerText = "Your email has been sent successfully!";
    document.getElementById('email-status').style.display = "block";

    document.getElementById('contact-form').reset();
  } else {
    document.getElementById('email-status').innerText = "Please fill in all the fields!";
    document.getElementById('email-status').style.display = "block";
    document.getElementById('email-status').style.color = "red";
  }
}
