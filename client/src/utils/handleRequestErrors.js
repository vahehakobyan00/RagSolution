import {message} from "antd";
import {requestErrorMessage} from "../config";


export const handleRequestErrors = (err, linkCheck = false) => {
    try {
        const { response } = err;
        console.log(response, 'err response');

        if (response) {
            const responseData = response.data;

            if (typeof responseData === "object" && Object.keys(responseData).length > 0) {
                if(typeof responseData?.responseDescription === 'string') {
                    message.error(responseData?.responseDescription)
                }
            } else if (typeof responseData === "string") {
                if (linkCheck) {
                    if(responseData.toLowerCase() === 'invalid token') {
                        message.error('Link is expired')
                    } else {
                        message.error('Invalid link or user not found')
                    }
                } else {
                    message.error(responseData);
                }
            } else {
                message.error(requestErrorMessage);
            }
        } else {
            message.error(requestErrorMessage);
        }
    } catch (error) {
        console.error("An error occurred while handling errors:", error);
        message.error(requestErrorMessage);
    }

};