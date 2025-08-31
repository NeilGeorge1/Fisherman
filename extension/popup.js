document.getElementById("checkBtn").addEventListener("click", async () => {
    const subject = document.getElementById("subject").value;
    const body = document.getElementById("body").value;

    if (!subject || !body) {
        document.getElementById("result").innerText = "Please enter subject and body.";
        return;
    }

    try {
        const formData = new FormData();
        formData.append("subject", subject);
        formData.append("body", body);

        const response = await fetch("http://localhost:5213/submit", {
            method: "POST",
            body: formData
        });

        const data = await response.json();
        document.getElementById("result").innerText = data.message;
    } catch (error) {
        document.getElementById("result").innerText = "Error connecting to API.";
    }
});
