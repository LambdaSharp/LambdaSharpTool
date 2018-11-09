class CustomError extends Error {
    constructor(message) {
        super();
        this.name = "CustomError";
        this.message = message;
    }
}

async function badFunction() {
    throw new CustomError("Oops!... I did it again");
}

exports.handler = async (event, context, callback) => {
    return await badFunction();
}

